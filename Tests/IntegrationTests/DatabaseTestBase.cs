using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PopfileNet.Backend;
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
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
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
