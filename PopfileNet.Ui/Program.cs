using PopfileNet.Ui.Components;
using PopfileNet.Ui.Services;
using PopfileNet.ServiceDefaults;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddFluentUIComponents();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var backendUrl = builder.Configuration["services:popfilenet-backend:http:0"] 
    ?? throw new InvalidOperationException("Backend service URL not configured");

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(backendUrl);
});

var app = builder.Build();


app.UseServiceDefaults();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
