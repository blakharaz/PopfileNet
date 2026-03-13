using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

    public async Task DisposeAsync()
    {
        Client?.Dispose();
    }

    protected static WebApplicationFactory<Program> CreateWebApplicationFactory(string connectionString)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                
                // Add connection string BEFORE the app is built so Program.Main sees it
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:popfilenet"] = connectionString,
                        ["ImapSettings:Server"] = "",
                        ["ImapSettings:Port"] = "993",
                        ["ImapSettings:Username"] = "",
                        ["ImapSettings:Password"] = "",
                        ["ImapSettings:UseSsl"] = "true",
                        ["SyncInterval"] = "01:00:00"
                    });
                });
            });
    }
}
