using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public sealed class ClassifierGroupExtensionsTests : IDisposable
{
    private readonly string _modelsRoot;
    private readonly ClaimsPrincipal _user;

    public ClassifierGroupExtensionsTests()
    {
        _modelsRoot = Path.Combine(Path.GetTempPath(), "popfilenet-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_modelsRoot);
        _user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "test-user")], "test"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_modelsRoot))
            Directory.Delete(_modelsRoot, recursive: true);
    }

    private static PopfileNetDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new PopfileNetDbContext(options);
    }

    private ClassifierManager CreateManager(string dbName)
    {
        var store = CreateStore(dbName);
        var classifierOptions = new ClassifierOptions { ModelsRoot = _modelsRoot };
        return new ClassifierManager(store, Microsoft.Extensions.Options.Options.Create(classifierOptions));
    }

    private EntityFrameworkClassifierModelStore CreateStore(string dbName)
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var factory = new TestDbContextFactory(options);
        return new EntityFrameworkClassifierModelStore(
            factory,
            Microsoft.Extensions.Options.Options.Create(new ClassifierOptions { ModelsRoot = _modelsRoot }));
    }

    private static Email CreateEmail(string id, string folderName, string bucketName = "Work")
    {
        return new Email
        {
            Id = id,
            Subject = $"Subject {id}",
            Folder = folderName,
            FolderNavigation = new MailFolder
            {
                Id = Guid.NewGuid().ToString(),
                Name = folderName,
                Bucket = new Bucket
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = bucketName
                }
            }
        };
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsTrainedFalseWhenNotTrained()
    {
        using var db = CreateContext(nameof(GetStatusAsync_ReturnsTrainedFalseWhenNotTrained));
        var manager = CreateManager(nameof(GetStatusAsync_ReturnsTrainedFalseWhenNotTrained));

        var result = await ClassifierGroupExtensions.GetStatusAsync(manager, _user);

        result.Value!.IsSuccess.ShouldBeTrue();
        result.Value.Value!.IsTrained.ShouldBeFalse();
        result.Value.Value.TrainingDataCount.ShouldBe(0);
    }

    [Fact]
    public async Task TrainAsync_WithNoData_ReturnsBadRequest()
    {
        using var db = CreateContext(nameof(TrainAsync_WithNoData_ReturnsBadRequest));
        var manager = CreateManager(nameof(TrainAsync_WithNoData_ReturnsBadRequest));

        var result = await ClassifierGroupExtensions.TrainAsync(db, manager, _user);

        var badRequest = result.ShouldBeOfType<BadRequest<ApiResponse<bool>>>();
        badRequest.Value!.IsSuccess.ShouldBeFalse();
        badRequest.Value.Error!.Code.ShouldBe("NO_TRAINING_DATA");
    }

    [Fact]
    public async Task TrainAsync_WithValidData_TrainsSuccessfully()
    {
        using var db = CreateContext(nameof(TrainAsync_WithValidData_TrainsSuccessfully));
        db.Emails.AddRange(
            CreateEmail("1", "Inbox", "Work"),
            CreateEmail("2", "Sent", "Personal"));
        await db.SaveChangesAsync();

        var manager = CreateManager(nameof(TrainAsync_WithValidData_TrainsSuccessfully));
        var result = await ClassifierGroupExtensions.TrainAsync(db, manager, _user);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<bool>>>();
        ok.Value!.IsSuccess.ShouldBeTrue();
        ok.Value.Value.ShouldBeTrue();

        var status = await ClassifierGroupExtensions.GetStatusAsync(manager, _user);
        status.Value!.Value!.IsTrained.ShouldBeTrue();
        status.Value.Value.TrainingDataCount.ShouldBe(2);
    }

    [Fact]
    public async Task TrainAsync_WithUnlabeledData_ReturnsBadRequest()
    {
        using var db = CreateContext(nameof(TrainAsync_WithUnlabeledData_ReturnsBadRequest));
        db.Emails.Add(new Email
        {
            Id = "1",
            Subject = "Test",
            FolderNavigation = null
        });
        await db.SaveChangesAsync();

        var manager = CreateManager(nameof(TrainAsync_WithUnlabeledData_ReturnsBadRequest));
        var result = await ClassifierGroupExtensions.TrainAsync(db, manager, _user);

        var badRequest = result.ShouldBeOfType<BadRequest<ApiResponse<bool>>>();
        badRequest.Value!.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task PredictAsync_WithUntrainedClassifier_ReturnsEmptyResult()
    {
        using var db = CreateContext(nameof(PredictAsync_WithUntrainedClassifier_ReturnsEmptyResult));
        var manager = CreateManager(nameof(PredictAsync_WithUntrainedClassifier_ReturnsEmptyResult));
        var request = new PredictRequest("test-id");

        var result = await ClassifierGroupExtensions.PredictAsync(request, db, manager, _user);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<PredictionResult>>>();
        ok.Value!.IsSuccess.ShouldBeTrue();
        ok.Value.Value!.PredictedBucket.ShouldBe("");
        ok.Value.Value.Confidence.ShouldBe(0);
    }

    [Fact]
    public async Task PredictAsync_WithLoadingFromDisk_ReturnsPrediction()
    {
        using var db = CreateContext(nameof(PredictAsync_WithLoadingFromDisk_ReturnsPrediction));
        db.Emails.Add(CreateEmail("train-1", "Inbox", "Work"));
        await db.SaveChangesAsync();
        var manager = CreateManager(nameof(PredictAsync_WithLoadingFromDisk_ReturnsPrediction));

        var trainResult = await ClassifierGroupExtensions.TrainAsync(db, manager, _user);
        trainResult.ShouldBeOfType<Ok<ApiResponse<bool>>>();

        // Simulate a restart: a fresh manager (empty cache, same store) must load the model from disk on demand.
        var freshManager = CreateManager(nameof(PredictAsync_WithLoadingFromDisk_ReturnsPrediction));
        var status = await ClassifierGroupExtensions.GetStatusAsync(freshManager, _user);
        status.Value!.Value!.IsTrained.ShouldBeTrue();

        db.Emails.Add(CreateEmail("predict-1", "Inbox", "Work"));
        await db.SaveChangesAsync();
        var request = new PredictRequest("predict-1");
        var result = await ClassifierGroupExtensions.PredictAsync(request, db, freshManager, _user);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<PredictionResult>>>();
        ok.Value!.IsSuccess.ShouldBeTrue();
        ok.Value.Value!.PredictedBucket.ShouldBe("Work");
    }

    private sealed class TestDbContextFactory(DbContextOptions<PopfileNetDbContext> options)
        : IDbContextFactory<PopfileNetDbContext>
    {
        public PopfileNetDbContext CreateDbContext() => new(options);
    }
}