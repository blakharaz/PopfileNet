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

public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected readonly DatabaseFixture Fixture;
    protected HttpClient? Client;
    protected WebApplicationFactory<Program>? Factory;

    protected DatabaseTestBase(DatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
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

    protected static WebApplicationFactory<Program> CreateWebApplicationFactory(string connectionString)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:popfilenet"] = connectionString
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.AddDbContext<PopfileNetDbContext>(options =>
                    {
                        options.UseNpgsql(connectionString);
                    });
                });
            });
    }
}
