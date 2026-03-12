using Microsoft.EntityFrameworkCore;
using PopfileNet.Database;
using Testcontainers.PostgreSql;
using Xunit;

namespace PopfileNet.Database.IntegrationTests;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("popfilenet")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public PopfileNetDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new PopfileNetDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public async Task ClearTablesAsync()
    {
        await using var context = CreateDbContext();
        try
        {
            var tables = new[] { "\"Emails\"", "\"MailFolders\"", "\"Buckets\"", "\"EmailHeaders\"" };
            foreach (var table in tables)
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {table} CASCADE");
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }
}

[CollectionDefinition("DatabaseTests")]
public class DatabaseTestsCollection : ICollectionFixture<TestDatabaseFixture>
{
}
