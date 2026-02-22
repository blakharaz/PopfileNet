using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Services;
using PopfileNet.Database;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, PopfileNet.Backend.Models.AppJsonSerializerContext.Default);
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<PopfileNetDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Database=PopfileNet;Trusted_Connection=True;MultipleActiveResultSets=true";
    options.UseSqlServer(connectionString);
});

builder.Services.Configure<ImapSettings>(builder.Configuration.GetSection("ImapSettings"));
builder.Services.AddScoped<IImapService, ImapService>();

var app = builder.Build();

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