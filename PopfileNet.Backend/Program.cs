using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.BackgroundServices;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.Database.Maintenance;
using PopfileNet.Database.Repositories;
using InvalidDataException = System.IO.InvalidDataException;

using PopfileNet.Imap;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;
using PopfileNet.ServiceDefaults;

[ExcludeFromCodeCoverage]
public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0,
                PopfileNet.Backend.Models.AppJsonSerializerContext.Default);
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.AddNpgsqlDbContext<PopfileNetDbContext>("popfilenet");

        var imapSettingsDefaults = builder.Configuration.GetSection("ImapSettings").Get<ImapSettings>()
                                   ?? throw new InvalidDataException("Missing IMAP settings in app configuration");
        builder.Services.AddSingleton(imapSettingsDefaults);

        builder.Services.AddScoped<IImapClientFactory, ImapClientFactory>();
        builder.Services.AddScoped<IImapService, ImapService>();
        builder.Services.AddScoped<ISettingsService, SettingsService>();
        builder.Services.AddScoped<IDatabaseFacade, EfCoreDatabaseFacadeWrapper>();
        builder.Services.AddScoped<IEmailRepository, EmailRepository>();
        builder.Services.AddScoped<IMigrationChecker, MigrationChecker>();
        builder.Services.AddHostedService<EmailSyncBackgroundService>();

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

        app.AddSettingsGroup()
            .AddJobsGroup()
            .AddMailsGroup()
            .AddClassifierGroup()
            .AddCategoriesGroup()
            .AddAccountsGroup();

        await app.RunAsync();
    }
}