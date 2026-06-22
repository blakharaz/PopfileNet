using System.Net;
using PopfileNet.Ui.Components;
using PopfileNet.Ui.Services;
using PopfileNet.ServiceDefaults;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddFluentUIComponents();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthorizationCore();

var backendUrl = builder.Configuration["services:popfilenet-backend:http:0"] 
    ?? throw new InvalidOperationException("Backend service URL not configured");

builder.Services.AddScoped(_ =>
{
    var handler = new SocketsHttpHandler
    {
        UseCookies = true,
        CookieContainer = new CookieContainer()
    };
    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri(backendUrl)
    };
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    return client;
});

builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<IApiClient>(sp => sp.GetRequiredService<ApiClient>());
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());

var app = builder.Build();


app.UseServiceDefaults();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
