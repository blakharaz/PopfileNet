using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public sealed class EntityFrameworkClassifierModelStoreTests : IDisposable
{
    private readonly string _modelsRoot;
    private readonly EntityFrameworkClassifierModelStore _store;

    public EntityFrameworkClassifierModelStoreTests()
    {
        _modelsRoot = Path.Combine(Path.GetTempPath(), "popfilenet-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_modelsRoot);
        _store = CreateStore(_modelsRoot, "store-db");
    }

    public void Dispose()
    {
        if (Directory.Exists(_modelsRoot))
            Directory.Delete(_modelsRoot, recursive: true);
    }

    private static EntityFrameworkClassifierModelStore CreateStore(string modelsRoot, string dbName)
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new EntityFrameworkClassifierModelStore(
            new TestDbContextFactory(options),
            Microsoft.Extensions.Options.Options.Create(new ClassifierOptions { ModelsRoot = modelsRoot }));
    }

    [Fact]
    public async Task ExistsAsync_WhenNoModel_ReturnsFalse()
    {
        var result = await _store.ExistsAsync("owner-1");
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetMetaAsync_WhenNoModel_ReturnsNull()
    {
        var result = await _store.GetMetaAsync("owner-1");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task OpenReadAsync_WhenNoModel_ReturnsNull()
    {
        var result = await _store.OpenReadAsync("owner-1");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_WritesModelAndMeta_Succeeds()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(bytes);
        var meta = new ClassifierModelMeta
        {
            OwnerId = "owner-1",
            TrainingSampleCount = 42,
            TrainedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            FormatVersion = 1
        };

        await _store.SaveAsync("owner-1", stream, meta);
        stream.Position = 0;

        (await _store.ExistsAsync("owner-1")).ShouldBeTrue();

        var persistedMeta = await _store.GetMetaAsync("owner-1");
        persistedMeta.ShouldNotBeNull();
        persistedMeta.OwnerId.ShouldBe("owner-1");
        persistedMeta.TrainingSampleCount.ShouldBe(42);
        persistedMeta.FormatVersion.ShouldBe(1);

        await using var readStream = await _store.OpenReadAsync("owner-1");
        readStream.ShouldNotBeNull();
        using var ms = new MemoryStream();
        await readStream!.CopyToAsync(ms);
        ms.ToArray().ShouldBe(bytes);
    }

    [Fact]
    public async Task SaveAsync_ExistingOwner_UpdatesMeta()
    {
        var meta = new ClassifierModelMeta { OwnerId = "owner-1", TrainingSampleCount = 10 };
        await _store.SaveAsync("owner-1", new MemoryStream([1, 2]), meta);

        var updated = new ClassifierModelMeta { OwnerId = "owner-1", TrainingSampleCount = 25 };
        await _store.SaveAsync("owner-1", new MemoryStream([3, 4, 5]), updated);

        var persisted = await _store.GetMetaAsync("owner-1");
        persisted!.TrainingSampleCount.ShouldBe(25);
    }

    [Fact]
    public async Task SaveAsync_StoresModelOnDiskUnderOwnerDirectory()
    {
        const string owner = "owner 1";
        await _store.SaveAsync(owner, new MemoryStream([9]), new ClassifierModelMeta { OwnerId = owner });

        var path = _store.GetModelPath(owner);
        File.Exists(path).ShouldBeTrue();
        path.ShouldEndWith(Path.Combine("owner 1", "model.zip"));
        File.ReadAllBytes(path).ShouldBe([9]);
    }

    [Fact]
    public async Task DeleteAsync_RemovesModelAndMeta()
    {
        await _store.SaveAsync("owner-1", new MemoryStream([1]), new ClassifierModelMeta { OwnerId = "owner-1" });

        await _store.DeleteAsync("owner-1");

        (await _store.ExistsAsync("owner-1")).ShouldBeFalse();
        File.Exists(_store.GetModelPath("owner-1")).ShouldBeFalse();
    }

    [Fact]
    public async Task Owners_AreIsolated_OnDiskAndMetadata()
    {
        await _store.SaveAsync("owner-a", new MemoryStream([1]), new ClassifierModelMeta { OwnerId = "owner-a" });

        File.Exists(_store.GetModelPath("owner-a")).ShouldBeTrue();
        File.Exists(_store.GetModelPath("owner-b")).ShouldBeFalse();
        (await _store.ExistsAsync("owner-b")).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ExistsAsync_InvalidOwner_Throws(string? ownerId)
    {
        var action = async () => await _store.ExistsAsync(ownerId!);
        await action.ShouldThrowAsync<ArgumentException>();
    }

    private sealed class TestDbContextFactory(DbContextOptions<PopfileNetDbContext> options)
        : IDbContextFactory<PopfileNetDbContext>
    {
        public PopfileNetDbContext CreateDbContext() => new(options);
    }
}