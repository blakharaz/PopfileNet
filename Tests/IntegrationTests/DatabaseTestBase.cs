using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using PopfileNet.Backend;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public abstract class DatabaseTestBase : IAsyncLifetime
{
    protected readonly DatabaseFixture Fixture;
    protected HttpClient Client = null!;
    protected WebApplicationFactory<Program> Factory = null!;
    protected readonly string ModelsRoot = Path.Combine(
        Path.GetTempPath(), "popfilenet-it-" + Guid.NewGuid().ToString("N"));

    protected DatabaseTestBase(DatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
        Directory.CreateDirectory(ModelsRoot);
        await SetupClientAsync();

        using var scope = Factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!await authService.AnyUserExistsAsync())
        {
            await authService.CreateUserAsync("test@popfile.local", "testpassword123", "Admin");
        }
    }

    protected abstract Task SetupClientAsync();

    public virtual async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }
        if (Directory.Exists(ModelsRoot))
            Directory.Delete(ModelsRoot, recursive: true);
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
                        ["ConnectionStrings:popfilenet"] = connString,
                        ["AdminEmail"] = "test@popfile.local",
                        ["AdminPassword"] = "testpassword123"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.AddDbContext<PopfileNetDbContext>(options =>
                    {
                        options.UseNpgsql(connString);
                    });
                    services.AddSingleton(
                        Microsoft.Extensions.Options.Options.Create(new ClassifierOptions { ModelsRoot = ModelsRoot }));
                });
            });
    }
}
