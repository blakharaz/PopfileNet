using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Services;
using PopfileNet.Database;
using PopfileNet.Imap.Settings;
using PopfileNet.ServiceDefaults;
using Aspire.Hosting;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, PopfileNet.Backend.Models.AppJsonSerializerContext.Default);
});

builder.Services.AddEndpointsApiExplorer();

builder.AddNpgsqlDbContext<PopfileNetDbContext>("popfilenet");

var imapSettings = builder.Configuration.GetSection("ImapSettings").Get<ImapSettings>()
    ?? throw new InvalidOperationException("ImapSettings configuration not found");
builder.Services.AddSingleton(imapSettings);
builder.Services.AddScoped<IImapService, ImapService>();

var app = builder.Build();

app.UseServiceDefaults();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PopfileNetDbContext>();
    db.Database.EnsureCreated();
}

app.AddSettingsGroup()
    .AddJobsGroup()
    .AddMailsGroup()
    .AddClassifierGroup();

app.Run();