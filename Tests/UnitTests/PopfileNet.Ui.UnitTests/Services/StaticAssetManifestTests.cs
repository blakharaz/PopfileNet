using Microsoft.Extensions.Logging.Abstractions;
using PopfileNet.Ui.Services;
using Shouldly;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Services;

public class StaticAssetManifestTests
{
    private readonly static NullLoggerFactory LoggerFactory = new();

    [Fact]
    public async Task LoadContentRootsAsync_MissingManifest_ReturnsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), "popfilenet-missing-" + Guid.NewGuid().ToString("N") + ".json");

        var result = await StaticAssetManifest.LoadContentRootsAsync(LoggerFactory, path);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task LoadContentRootsAsync_ValidManifest_ReturnsContentRoots()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "manifest.json");
        await File.WriteAllTextAsync(manifestPath, """{ "ContentRoots": ["/root/a", "/root/framework", "/root/fluent"] }""");

        var result = await StaticAssetManifest.LoadContentRootsAsync(LoggerFactory, manifestPath);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result["0"].ShouldBe("/root/a");
        result["1"].ShouldBe("/root/framework");
        result["2"].ShouldBe("/root/fluent");
    }

    [Fact]
    public async Task LoadContentRootsAsync_InvalidJson_ReturnsEmpty()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "manifest.json");
        await File.WriteAllTextAsync(manifestPath, "not valid json");

        var result = await StaticAssetManifest.LoadContentRootsAsync(LoggerFactory, manifestPath);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task LoadContentRootsAsync_MissingContentRootsProperty_ReturnsEmpty()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "manifest.json");
        await File.WriteAllTextAsync(manifestPath, """{ "SomethingElse": [] }""");

        var result = await StaticAssetManifest.LoadContentRootsAsync(LoggerFactory, manifestPath);

        result.ShouldBeEmpty();
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "popfilenet-manifest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}