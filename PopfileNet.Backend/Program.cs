using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, PopfileNet.Backend.Models.AppJsonSerializerContext.Default);
});

builder.Services.AddEndpointsApiExplorer();

builder.AddNpgsqlDbContext<PopfileNetDbContext>("popfilenet");

builder.Services.AddScoped<PopfileNet.Common.IImapService, ImapService>();
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
    db.Database.Migrate();
}

app.AddSettingsGroup()
    .AddJobsGroup()
    .AddMailsGroup()
    .AddClassifierGroup()
    .AddCategoriesGroup()
    .AddAccountsGroup();

app.Run();