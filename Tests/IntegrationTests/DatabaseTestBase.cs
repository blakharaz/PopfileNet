using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PopfileNet.Backend;
using PopfileNet.Database;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected readonly DatabaseFixture Fixture;
    protected HttpClient Client = null!;
    protected WebApplicationFactory<Program> Factory = null!;

    protected DatabaseTestBase(DatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
        await SetupClientAsync();
    }

    protected abstract Task SetupClientAsync();

    public virtual async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }
    }

    protected WebApplicationFactory<Program> CreateWebApplicationFactory(string? connectionString = null)
    {
        var connString = connectionString ?? Fixture.ConnectionString;
        
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:popfilenet"] = connString
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.AddDbContext<PopfileNetDbContext>(options =>
                    {
                        options.UseNpgsql(connString);
                    });
                });
            });
    }
}
