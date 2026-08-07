namespace PopfileNet.Ui.Services;

// Serves legacy JS/CSS static assets using the content roots resolved from the
// build-generated runtime manifest, working around the .NET 10 MapStaticAssets bug.
public class StaticAssetServingMiddleware(
    RequestDelegate next,
    ILogger<StaticAssetServingMiddleware> logger,
    Dictionary<string, string> contentRoots)
{
    private const string FrameworkIndex = "4";
    private const string FluentUiIndex = "5";
    private const string FluentUiPackageId = "Microsoft.FluentUI.AspNetCore.Components";

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (path == null)
        {
            await next(context);
            return;
        }

        try
        {
            if (TryResolveFile(path, out var filePath))
            {
                context.Response.ContentType = GetContentType(filePath);
                await context.Response.SendFileAsync(filePath, context.RequestAborted);
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error serving static {Path}: {Message}", path, ex.Message);
        }

        await next(context);
    }

    private bool TryResolveFile(string path, out string filePath)
    {
        filePath = string.Empty;
        var (basePath, relPath) = ResolveContentPath(path);

        if (basePath == null || relPath == null)
        {
            return false;
        }

        var queryIndex = relPath.IndexOf('?');
        if (queryIndex >= 0)
        {
            relPath = relPath[..queryIndex];
        }

        var candidate = Path.Combine(basePath, relPath);
        if (!File.Exists(candidate))
        {
            return false;
        }

        filePath = candidate;
        return true;
    }

    private (string? basePath, string? relPath) ResolveContentPath(string path)
    {
        if (path.StartsWith("/_framework/"))
        {
            // ContentRootIndex 4 from the build manifest points to the framework assets
            if (contentRoots.TryGetValue(FrameworkIndex, out var frameworkPath))
            {
                return (frameworkPath, path[12..]);
            }
        }
        else if (path.StartsWith("/_content/"))
        {
            // ContentRootIndex 5 from the build manifest points to FluentUI assets
            var afterContent = path[10..];
            var slashIndex = afterContent.IndexOf('/');
            if (slashIndex > 0)
            {
                var packageId = afterContent[..slashIndex];
                if (packageId == FluentUiPackageId &&
                    contentRoots.TryGetValue(FluentUiIndex, out var fluentUiPath))
                {
                    return (fluentUiPath, afterContent[(slashIndex + 1)..]);
                }
            }
        }

        return (null, null);
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".js" => "text/javascript",
            ".css" => "text/css",
            ".json" => "application/json",
            ".map" => "application/json",
            ".txt" => "text/plain",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}