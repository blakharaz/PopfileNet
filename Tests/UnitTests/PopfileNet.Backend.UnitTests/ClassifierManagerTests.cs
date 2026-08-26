using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Services;
using PopfileNet.Classifier;
using PopfileNet.Common;
using PopfileNet.Database;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public sealed class ClassifierManagerTests : IDisposable
{
    private readonly string _modelsRoot;
    private readonly ClassifierManager _manager;
    private readonly ClassifierOptions _options;

    public ClassifierManagerTests()
    {
        _modelsRoot = Path.Combine(Path.GetTempPath(), "popfilenet-manager-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_modelsRoot);
        _options = new ClassifierOptions { ModelsRoot = _modelsRoot };
        _manager = new ClassifierManager(CreateStore(), Microsoft.Extensions.Options.Options.Create(_options));
    }

    public void Dispose()
    {
        if (Directory.Exists(_modelsRoot))
            Directory.Delete(_modelsRoot, recursive: true);
    }

    private EntityFrameworkClassifierModelStore CreateStore(string dbName = "manager-db")
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new EntityFrameworkClassifierModelStore(
            new TestDbContextFactory(options),
            Microsoft.Extensions.Options.Options.Create(new ClassifierOptions { ModelsRoot = _modelsRoot }));
    }

    private static NaiveBayesianClassifier CreateTrainedClassifier()
    {
        var classifier = new NaiveBayesianClassifier();
        var dataSet = new EmailClassificationDataSet();
        dataSet.AddMail(CreateEmail("Newsletter", "Buy our products now"), "spam");
        dataSet.AddMail(CreateEmail("Meeting", "Let's schedule a meeting"), "ham");
        classifier.Train(dataSet);
        return classifier;
    }

    private static Email CreateEmail(string subject, string body) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Subject = subject,
        Body = body,
        FromAddress = "test@example.com",
        ToAddresses = "recipient@example.com",
        ReceivedDate = DateTime.Now
    };

    [Fact]
    public async Task GetModelAsync_NoModel_ReturnsNull()
    {
        var result = await _manager.GetModelAsync("unknown-owner");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveThenGet_CachesAndReturnsModel()
    {
        var classifier = CreateTrainedClassifier();
        await _manager.SaveModelAsync("owner-1", classifier);

        var first = await _manager.GetModelAsync("owner-1");
        var second = await _manager.GetModelAsync("owner-1");

        first.ShouldNotBeNull();
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public async Task SaveModelAsync_UntrainedClassifier_Throws()
    {
        var action = async () => await _manager.SaveModelAsync("owner-1", new NaiveBayesianClassifier());
        await action.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetModelAsync_FreshManager_LoadsFromStore()
    {
        await _manager.SaveModelAsync("owner-1", CreateTrainedClassifier());

        var freshStore = CreateStore();
        var freshManager = new ClassifierManager(freshStore, Microsoft.Extensions.Options.Options.Create(_options));

        var model = await freshManager.GetModelAsync("owner-1");
        model.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetModelAsync_LoadedModel_CanPredict()
    {
        await _manager.SaveModelAsync("owner-1", CreateTrainedClassifier());

        var model = await _manager.GetModelAsync("owner-1");
        var prediction = model!.Predict(CreateEmail("New meeting", "Let's meet tomorrow"));

        prediction.PredictedLabel.ShouldNotBeNullOrEmpty();
        prediction.Scores.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Invalidate_ForcesReload()
    {
        await _manager.SaveModelAsync("owner-1", CreateTrainedClassifier());
        var first = await _manager.GetModelAsync("owner-1");

        _manager.Invalidate("owner-1");

        var second = await _manager.GetModelAsync("owner-1");
        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.ShouldNotBeSameAs(second);
    }

    [Fact]
    public async Task GetMetaAsync_AfterSave_ReturnsMetadata()
    {
        var classifier = CreateTrainedClassifier();
        await _manager.SaveModelAsync("owner-1", classifier);

        var meta = await _manager.GetMetaAsync("owner-1");
        meta.ShouldNotBeNull();
        meta!.OwnerId.ShouldBe("owner-1");
        meta.TrainingSampleCount.ShouldBe(classifier.TrainingSampleCount);
    }

    [Fact]
    public async Task TenantIsolation_DifferentOwners_ReturnDistinctInstances()
    {
        await _manager.SaveModelAsync("owner-a", CreateTrainedClassifier());
        await _manager.SaveModelAsync("owner-b", CreateTrainedClassifier());

        var a = await _manager.GetModelAsync("owner-a");
        var b = await _manager.GetModelAsync("owner-b");

        a.ShouldNotBeNull();
        b.ShouldNotBeNull();
        a.ShouldNotBeSameAs(b);
    }

    [Fact]
    public async Task Evict_IdleBeyondTtl_RemovesEntryWhenClockAdvances()
    {
        var now = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var manager = new ClassifierManager(
            CreateStore(), Microsoft.Extensions.Options.Options.Create(_options), () => now);

        await manager.SaveModelAsync("owner-1", CreateTrainedClassifier());

        // TTL configured as 20 minutes: nothing evicted within the TTL window.
        manager.Evict();
        manager.CacheCount.ShouldBe(1);

        // Advance the clock past the TTL and evict: the idle entry must be removed.
        now = now.AddMinutes(21);
        manager.Evict();
        manager.CacheCount.ShouldBe(0);
    }

    [Fact]
    public async Task Evict_ExceedingCapacity_EvictsLeastRecentlyUsed()
    {
        var now = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var options = new ClassifierOptions { ModelsRoot = _modelsRoot, MaxCachedModels = 2 };
        var manager = new ClassifierManager(
            CreateStore(), Microsoft.Extensions.Options.Options.Create(options), () => now);

        await manager.SaveModelAsync("owner-1", CreateTrainedClassifier());
        now = now.AddMinutes(1);
        await manager.SaveModelAsync("owner-2", CreateTrainedClassifier());
        now = now.AddMinutes(1);
        await manager.SaveModelAsync("owner-3", CreateTrainedClassifier());

        manager.Evict();

        // Capacity 2: the least-recently-used entry (owner-1) is evicted.
        manager.CacheCount.ShouldBe(2);
        (await manager.GetModelAsync("owner-1")).ShouldNotBeNull(); // reloadable from store
        manager.CacheCount.ShouldBe(2);
    }

    [Fact]
    public async Task Evict_ZeroTtl_EvictsAllIdleEntriesImmediately()
    {
        var manager = new ClassifierManager(
            CreateStore(),
            Microsoft.Extensions.Options.Options.Create(new ClassifierOptions { ModelsRoot = _modelsRoot, CacheTtl = TimeSpan.Zero }),
            () => new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc));

        await manager.SaveModelAsync("owner-1", CreateTrainedClassifier());
        manager.Evict();

        manager.CacheCount.ShouldBe(0);
    }

    private sealed class TestDbContextFactory(DbContextOptions<PopfileNetDbContext> options)
        : IDbContextFactory<PopfileNetDbContext>
    {
        public PopfileNetDbContext CreateDbContext() => new(options);
    }
}