using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using PopfileNet.Backend.BackgroundServices;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.DevMode;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.Database.Maintenance;
using PopfileNet.Database.Repositories;
using PopfileNet.Imap;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;
using PopfileNet.ServiceDefaults;

namespace PopfileNet.Backend;

[ExcludeFromCodeCoverage]
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0,
                Models.AppJsonSerializerContext.Default);
        });

        var devMode = builder.Configuration.GetSection("DevMode").Get<DevModeSettings>() ??
                      new DevModeSettings(Enabled: false);
        builder.Services.AddSingleton(devMode);

        builder.Services.AddEndpointsApiExplorer();
        builder.AddNpgsqlDbContext<PopfileNetDbContext>("popfilenet", configureDbContextOptions: options =>
        {
            if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Test")
            {
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        });

        var imapSettingsDefaults = builder.Configuration.GetSection("ImapSettings").Get<ImapSettings>()
                                   ?? throw new InvalidDataException("Missing IMAP settings in app configuration");
        builder.Services.AddSingleton(imapSettingsDefaults);

        builder.Services.AddScoped<IImapClientFactory, ImapClientFactory>();
        builder.Services.AddScoped<IImapService, ImapService>();
        builder.Services.AddScoped<ISettingsService, SettingsService>();
        builder.Services.AddScoped<IDatabaseFacade, EfCoreDatabaseFacadeWrapper>();
        builder.Services.AddScoped<IEmailRepository, EmailRepository>();
        builder.Services.AddScoped<IMigrationChecker, MigrationChecker>();
        builder.Services.AddScoped<ClassifierEvaluationService>();
        builder.Services.AddScoped<IClassifierDataProvider, ClassifierDataProvider>();
        builder.Services.AddHostedService<EmailSyncBackgroundService>();

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<PopfileNetDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/auth/login";
                options.LogoutPath = "/auth/logout";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.RequireRole("Admin"));
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddHttpContextAccessor();

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/")
            {
                context.Response.Redirect("/mails");
                return;
            }

            await next();
        });

        app.UseServiceDefaults();

        app.UseAuthentication();
        app.UseAuthorization();

        using (var scope = app.Services.CreateScope())
        {
            var migrationChecker = scope.ServiceProvider.GetRequiredService<IMigrationChecker>();

            var hasLegacy = await migrationChecker.HasLegacyTablesAsync();
            if (hasLegacy)
            {
                throw new InvalidOperationException(
                    "Database exists, but is in legacy format. Please delete the existing database and restart the application.");
            }

            if (await migrationChecker.HasPendingMigrationsAsync())
            {
                await migrationChecker.ApplyMigrationsAsync();
            }
        }

        app.AddEvaluationGroup()
            .AddAuthGroup()
            .AddSettingsGroup()
            .AddJobsGroup()
            .AddMailsGroup()
            .AddClassifierGroup()
            .AddCategoriesGroup()
            .AddAccountsGroup();

        var adminEmail = builder.Configuration["AdminEmail"] ?? "";
        var adminPassword = builder.Configuration["AdminPassword"] ?? "";

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "AdminEmail and AdminPassword must be configured. Set ADMIN_EMAIL and ADMIN_PASSWORD environment variables or add them to appsettings.json.");
        }

        await SeedAdminUserAsync(app.Services, adminEmail, adminPassword);

        await app.RunAsync();
    }

    private static async Task SeedAdminUserAsync(IServiceProvider services, string adminEmail, string adminPassword)
    {
        using var scope = services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        if (await authService.AnyUserExistsAsync())
        {
            return;
        }

        try
        {
            await authService.CreateUserAsync(adminEmail, adminPassword, "Admin");
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Default admin user created");
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Failed to seed admin user");
        }
    }
}