using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.Imap;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;
using PopfileNet.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, PopfileNet.Backend.Models.AppJsonSerializerContext.Default);
});

builder.Services.AddEndpointsApiExplorer();

builder.AddNpgsqlDbContext<PopfileNetDbContext>("popfilenet");

var imapSettingsDefaults = builder.Configuration.GetSection("ImapSettings").Get<ImapSettings>()
    ?? new ImapSettings { Server = "", Username = "", Password = "", Port = 993, UseSsl = true, MaxParallelConnections = 4 };
builder.Services.AddSingleton(imapSettingsDefaults);

builder.Services.AddScoped<IImapClientFactory, ImapClientFactory>();
builder.Services.AddScoped<IImapService, ImapService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

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
    var db = scope.ServiceProvider.GetRequiredService<PopfileNetDbContext>();
    
    if (!await db.Database.CanConnectAsync())
    {
        return;
    }
    
    var historyTableExists = await db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*)::int FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory'"
    ).FirstOrDefaultAsync() > 0;
    
    if (!historyTableExists)
    {
        var hasAnyTables = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*)::int FROM information_schema.tables WHERE table_schema = 'public' AND table_name NOT LIKE '__%'"
        ).FirstOrDefaultAsync() > 0;
        
        if (hasAnyTables)
        {
            throw new InvalidOperationException(
                "Database exists, but is in legacy format. Please delete the existing database and restart the application.");
        }
    }
    
    var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        await db.Database.MigrateAsync();
    }
}

app.AddSettingsGroup()
    .AddJobsGroup()
    .AddMailsGroup()
    .AddClassifierGroup()
    .AddCategoriesGroup()
    .AddAccountsGroup();

app.Run();