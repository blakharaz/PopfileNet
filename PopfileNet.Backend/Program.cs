using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
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

var imapSettings = builder.Configuration.GetSection("ImapSettings").Get<ImapSettings>()
    ?? new ImapSettings { Server = "imap.example.com", Username = "", Password = "" };
builder.Services.AddSingleton(imapSettings);
builder.Services.AddScoped<IImapService, ImapService>();

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
    db.Database.EnsureCreated();
}

app.AddSettingsGroup()
    .AddJobsGroup()
    .AddMailsGroup()
    .AddClassifierGroup()
    .AddCategoriesGroup()
    .AddAccountsGroup();

app.Run();