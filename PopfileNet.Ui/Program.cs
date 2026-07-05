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

// Workaround for .NET 10 bug: MapStaticAssets returns empty MIME type for JS files.
// Read content roots from the build-generated runtime manifest to get correct paths
// regardless of where NuGet packages are cached.
var assetsRuntimePath = System.IO.Path.Combine(
    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
    "PopfileNet.Ui.staticwebassets.runtime.json");
Dictionary<string, string> contentRoots = new();
if (System.IO.File.Exists(assetsRuntimePath))
{
    try
    {
        var json = System.Text.Json.JsonDocument.Parse(
            System.IO.File.ReadAllText(assetsRuntimePath));
        var roots = json.RootElement.GetProperty("ContentRoots");
        for (int i = 0; i < roots.GetArrayLength(); i++)
        {
            var root = roots[i].GetString();
            if (root != null) contentRoots[i.ToString()] = root;
        }
    }
    catch { }
}
app.Use(async (ctx, next) =>
{
    var p = ctx.Request.Path.Value;
    if (p == null) { await next(); return; }
    try
    {
        string? basePath = null;
        string? relPath = null;
        if (p.StartsWith("/_framework/"))
        {
            // ContentRootIndex 4 from the build manifest points to the framework assets
            if (contentRoots.TryGetValue("4", out var fwPath))
            {
                basePath = fwPath;
                relPath = p[12..];
            }
        }
        else if (p.StartsWith("/_content/"))
        {
            // ContentRootIndex 5 from the build manifest points to FluentUI assets
            var afterContent = p[10..];
            var slashIdx = afterContent.IndexOf('/');
            if (slashIdx > 0)
            {
                var packageId = afterContent[..slashIdx];
                if (packageId == "Microsoft.FluentUI.AspNetCore.Components" &&
                    contentRoots.TryGetValue("5", out var fluPath))
                {
                    basePath = fluPath;
                    relPath = afterContent[(slashIdx + 1)..];
                }
            }
        }
        if (basePath != null && relPath != null)
        {
            var qIdx = relPath.IndexOf('?');
            if (qIdx >= 0) relPath = relPath[..qIdx];
            var fp = System.IO.Path.Combine(basePath, relPath);
            if (System.IO.File.Exists(fp))
            {
                var ext = System.IO.Path.GetExtension(fp).ToLowerInvariant();
                ctx.Response.ContentType = ext switch
                {
                    ".js" => "text/javascript",
                    ".css" => "text/css",
                    ".json" => "application/json",
                    ".map" => "application/json",
                    ".txt" => "text/plain",
                    ".png" => "image/png",
                    _ => "application/octet-stream"
                };
                await ctx.Response.SendFileAsync(fp);
                return;
            }
        }
    }
    catch (Exception ex)
    {
        System.Console.WriteLine($"[FX] Error serving {p}: {ex.Message}");
    }
    await next();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
