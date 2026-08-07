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
builder.Services.AddHttpContextAccessor();

var backendUrl = builder.Configuration["services:popfilenet-backend:http:0"]
    ?? throw new InvalidOperationException("Backend service URL not configured");

builder.Services.AddScoped(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var handler = new CookieForwardingHandler(httpContextAccessor)
    {
        InnerHandler = new SocketsHttpHandler()
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

var contentRoots = await StaticAssetManifest.LoadContentRootsAsync(app.Services.GetRequiredService<ILoggerFactory>());
app.UseMiddleware<StaticAssetServingMiddleware>(contentRoots);

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
