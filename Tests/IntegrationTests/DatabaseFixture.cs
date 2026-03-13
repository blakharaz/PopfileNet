using Microsoft.EntityFrameworkCore;
using Npgsql;
using PopfileNet.Database;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Xunit;

namespace PopfileNet.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; } = null!;
    private string? _connectionString;
    public string ConnectionString => _connectionString ?? throw new InvalidOperationException("Database not initialized");
    
    private Respawner? _respawner;

    public async Task InitializeAsync()
    {
        Postgres = new PostgreSqlBuilder(image: "postgres:16-alpine")
            .WithDatabase("popfilenet")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await Postgres.StartAsync();

        _connectionString = Postgres.GetConnectionString();
        
        await InitializeDatabaseAsync();
        await InitializeRespawnerAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        var optionsBuilder = new DbContextOptionsBuilder<PopfileNetDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString);
        
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
            WithReseed = false
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
        await Postgres.DisposeAsync();
    }
}