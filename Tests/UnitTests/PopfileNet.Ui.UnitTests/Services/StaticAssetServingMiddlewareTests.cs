using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PopfileNet.Ui.Services;
using Shouldly;
using System.IO.Pipelines;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Services;

public class StaticAssetServingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RequestForExistingFrameworkJs_ServesFileAndStopsPipeline()
    {
        var tempDir = CreateTempDir();
        var root = Path.Combine(tempDir, "framework");
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "blazor.web.js");
        await File.WriteAllTextAsync(filePath, "console.log('hi')");

        var contentRoots = new Dictionary<string, string> { ["4"] = root };
        var pipelineContinued = false;
        Task Next(HttpContext context)
        {
            pipelineContinued = true;
            return Task.CompletedTask;
        }

        var middleware = new StaticAssetServingMiddleware(Next, NullLogger<StaticAssetServingMiddleware>.Instance, contentRoots);
        var context = new DefaultHttpContext();
        context.Request.Path = "/_framework/blazor.web.js";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        pipelineContinued.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(200);
        context.Response.ContentType.ShouldBe("text/javascript");
    }

    [Fact]
    public async Task InvokeAsync_RequestForNonManagedPath_CallsNext()
    {
        var middleware = new StaticAssetServingMiddleware(Next, NullLogger<StaticAssetServingMiddleware>.Instance, []);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/values";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(404);
        context.Response.ContentType.ShouldBeNull();
    }

    [Fact]
    public async Task InvokeAsync_RequestForMissingFile_CallsNext()
    {
        var tempDir = CreateTempDir();
        var contentRoots = new Dictionary<string, string> { ["5"] = tempDir };
        var middleware = new StaticAssetServingMiddleware(Next, NullLogger<StaticAssetServingMiddleware>.Instance, contentRoots);
        var context = new DefaultHttpContext();
        context.Request.Path = "/_content/Microsoft.FluentUI.AspNetCore.Components/missing.css";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(404);
        context.Response.ContentType.ShouldBeNull();
    }

    [Fact]
    public async Task InvokeAsync_RequestWithQueryString_StripsQueryBeforeServing()
    {
        var tempDir = CreateTempDir();
        var root = Path.Combine(tempDir, "fluent");
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "app.css");
        await File.WriteAllTextAsync(filePath, "body{}");

        var contentRoots = new Dictionary<string, string> { ["5"] = root };
        var middleware = new StaticAssetServingMiddleware(Next, NullLogger<StaticAssetServingMiddleware>.Instance, contentRoots);
        var context = new DefaultHttpContext();
        context.Request.Path = "/_content/Microsoft.FluentUI.AspNetCore.Components/app.css?v=123";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(200);
        context.Response.ContentType.ShouldBe("text/css");
    }

    [Fact]
    public async Task InvokeAsync_WithNullPath_CallsNext()
    {
        var pipelineContinued = false;
        Task Next(HttpContext context)
        {
            pipelineContinued = true;
            return Task.CompletedTask;
        }

        var requestMock = new Mock<HttpRequest>();
        requestMock.Setup(r => r.Path).Returns(default(PathString));
        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(c => c.Request).Returns(requestMock.Object);

        var middleware = new StaticAssetServingMiddleware(Next, NullLogger<StaticAssetServingMiddleware>.Instance, []);

        await middleware.InvokeAsync(contextMock.Object);

        pipelineContinued.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_FrameworkPathWithoutRootKey_CallsNext()
    {
        var tempDir = CreateTempDir();
        var root = Path.Combine(tempDir, "framework");
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "blazor.web.js");
        await File.WriteAllTextAsync(filePath, "console.log('hi')");

        var contentRoots = new Dictionary<string, string> { ["5"] = tempDir };
        var middleware = new StaticAssetServingMiddleware(Next, NullLogger<StaticAssetServingMiddleware>.Instance, contentRoots);
        var context = new DefaultHttpContext();
        context.Request.Path = "/_framework/blazor.web.js";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task InvokeAsync_ContentPathWithNonFluentPackage_CallsNext()
    {
        var tempDir = CreateTempDir();
        var root = Path.Combine(tempDir, "other");
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "style.css");
        await File.WriteAllTextAsync(filePath, "body{}");

        var contentRoots = new Dictionary<string, string> { ["5"] = root };
        var middleware = new StaticAssetServingMiddleware(Next, NullLogger<StaticAssetServingMiddleware>.Instance, contentRoots);
        var context = new DefaultHttpContext();
        context.Request.Path = "/_content/Some.Other.Package/style.css";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task InvokeAsync_ContentPathWithoutSlash_CallsNext()
    {
        var contentRoots = new Dictionary<string, string> { ["5"] = CreateTempDir() };
        var middleware = new StaticAssetServingMiddleware(Next, NullLogger<StaticAssetServingMiddleware>.Instance, contentRoots);
        var context = new DefaultHttpContext();
        context.Request.Path = "/_content/Microsoft.FluentUI.AspNetCore.ComponentsX";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task InvokeAsync_SendFileThrows_LogsAndCallsNext()
    {
        var tempDir = CreateTempDir();
        var root = Path.Combine(tempDir, "framework");
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "blazor.web.js");
        await File.WriteAllTextAsync(filePath, "console.log('hi')");

        var contentRoots = new Dictionary<string, string> { ["4"] = root };
        var pipelineContinued = false;
        Task Next(HttpContext context)
        {
            pipelineContinued = true;
            return Task.CompletedTask;
        }

        var logger = new TestLogger();
        var middleware = new StaticAssetServingMiddleware(Next, logger, contentRoots);
        var context = new DefaultHttpContext();
        context.Request.Path = "/_framework/blazor.web.js";
        context.Features.Set<IHttpResponseBodyFeature>(new ThrowingSendFileFeature());

        await middleware.InvokeAsync(context);

        pipelineContinued.ShouldBeTrue();
        logger.HasError.ShouldBeTrue();
    }

    [Theory]
    [InlineData("app.json", "application/json")]
    [InlineData("blazor.web.js.map", "application/json")]
    [InlineData("readme.txt", "text/plain")]
    [InlineData("icon.png", "image/png")]
    [InlineData("asset.woff", "application/octet-stream")]
    public async Task InvokeAsync_ContentTypeMapping_ServesCorrectType(string fileName, string expectedContentType)
    {
        var tempDir = CreateTempDir();
        var root = Path.Combine(tempDir, "fluent");
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, fileName);
        await File.WriteAllTextAsync(filePath, "content");

        var contentRoots = new Dictionary<string, string> { ["5"] = root };
        var middleware = new StaticAssetServingMiddleware(Next, NullLogger<StaticAssetServingMiddleware>.Instance, contentRoots);
        var context = new DefaultHttpContext();
        context.Request.Path = $"/_content/Microsoft.FluentUI.AspNetCore.Components/{fileName}";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(200);
        context.Response.ContentType.ShouldBe(expectedContentType);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "popfilenet-ms" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static Task Next(HttpContext context)
    {
        context.Response.StatusCode = 404;
        return Task.CompletedTask;
    }

    private sealed class TestLogger : ILogger<StaticAssetServingMiddleware>
    {
        public bool HasError { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                HasError = true;
            }
        }
    }

    private sealed class ThrowingSendFileFeature : IHttpResponseBodyFeature
    {
        public Stream Stream => new MemoryStream();
        public PipeWriter Writer => PipeWriter.Create(Stream);

        public void DisableBuffering()
        {
        }

        public Task CompleteAsync()
        {
            throw new InvalidOperationException("boom");
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }
}