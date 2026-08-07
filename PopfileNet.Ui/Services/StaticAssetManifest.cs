using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace PopfileNet.Ui.Services;

// Workaround for .NET 10 bug: MapStaticAssets returns empty MIME type for JS files.
// Read content roots from the build-generated runtime manifest to get correct paths
// regardless of where NuGet packages are cached.
public static class StaticAssetManifest
{
    public static async Task<Dictionary<string, string>> LoadContentRootsAsync(
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        var assetsRuntimePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "PopfileNet.Ui.staticwebassets.runtime.json");

        return await LoadContentRootsAsync(loggerFactory, assetsRuntimePath, cancellationToken);
    }

    public static async Task<Dictionary<string, string>> LoadContentRootsAsync(
        ILoggerFactory loggerFactory,
        string assetsRuntimePath,
        CancellationToken cancellationToken = default)
    {

        var logger = loggerFactory.CreateLogger(typeof(StaticAssetManifest));
        var contentRoots = new Dictionary<string, string>();

        if (!File.Exists(assetsRuntimePath))
        {
            return contentRoots;
        }

        try
        {
            await using var stream = File.OpenRead(assetsRuntimePath);
            var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var roots = json.RootElement.GetProperty("ContentRoots");
            for (var i = 0; i < roots.GetArrayLength(); i++)
            {
                var root = roots[i].GetString();
                if (root != null)
                {
                    contentRoots[i.ToString()] = root;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load static web assets runtime manifest from {Path}", assetsRuntimePath);
        }

        return contentRoots;
    }
}