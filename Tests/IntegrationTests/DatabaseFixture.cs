using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using PopfileNet.Database;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Xunit;

namespace PopfileNet.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private static readonly Lazy<Task<DatabaseFixture>> LazyInstance = new(() => new DatabaseFixture().InitializeAsyncCore());
    
    public static DatabaseFixture Instance => LazyInstance.Value.Result;

    public PostgreSqlContainer? Postgres { get; private set; }

    private string? _connectionString;
    public string ConnectionString => _connectionString ?? throw new InvalidOperationException("Database not initialized");
    
    private Respawner? _respawner;
    private bool _disposed;

    private async Task<DatabaseFixture> InitializeAsyncCore()
    {
        Postgres = new PostgreSqlBuilder(image: "postgres:16-alpine")
            .WithDatabase("popfilenet")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await Postgres.StartAsync();

        _connectionString = Postgres.GetConnectionString();
        
        try
        {
            await InitializeDatabaseAsync();
            await InitializeRespawnerAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }

        return this;
    }

    public Task InitializeAsync() => InitializeAsyncCore();

    private async Task InitializeDatabaseAsync()
    {
        var optionsBuilder = new DbContextOptionsBuilder<PopfileNetDbContext>();
        optionsBuilder
            .UseNpgsql(ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        
        await using var dbContext = new PopfileNetDbContext(optionsBuilder.Options);
        
        var pending = await dbContext.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            await dbContext.Database.MigrateAsync();
        }
    }

    private async Task InitializeRespawnerAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
            WithReseed = false,
            DbAdapter = DbAdapter.Postgres
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner == null)
        {
            return;
        }
        
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        
        if (Postgres != null)
        {
            await Postgres.DisposeAsync();
        }
    }
}
