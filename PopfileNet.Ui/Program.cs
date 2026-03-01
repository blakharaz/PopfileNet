using PopfileNet.Ui.Components;
using PopfileNet.Ui.Services;
using PopfileNet.ServiceDefaults;
using Aspire.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddFluentUIComponents();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri("http://backend");
});

var app = builder.Build();


app.UseServiceDefaults();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
