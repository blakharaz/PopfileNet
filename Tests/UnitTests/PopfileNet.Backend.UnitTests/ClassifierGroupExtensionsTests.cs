using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Models;
using PopfileNet.Common;
using PopfileNet.Database;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public sealed class ClassifierGroupExtensionsTests
{
    private static PopfileNetDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new PopfileNetDbContext(options);
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
    public async Task Reset_ClearsClassifierState()
    {
        using var db = CreateContext(nameof(Reset_ClearsClassifierState));
        db.Emails.Add(CreateEmail("1", "Inbox", "Work"));
        await db.SaveChangesAsync();
        await ClassifierGroupExtensions.TrainAsync(db);

        ClassifierGroupExtensions.Reset();

        var status = ClassifierGroupExtensions.GetStatusAsync();
        status.Value!.Value!.IsTrained.ShouldBeFalse();
        status.Value.Value.TrainingDataCount.ShouldBe(0);
    }

    [Fact]
    public void GetStatusAsync_ReturnsTrainedFalseWhenNotTrained()
    {
        ClassifierGroupExtensions.Reset();
        var result = ClassifierGroupExtensions.GetStatusAsync();

        result.Value!.IsSuccess.ShouldBeTrue();
        result.Value.Value!.IsTrained.ShouldBeFalse();
        result.Value.Value.TrainingDataCount.ShouldBe(0);
    }

    [Fact]
    public async Task TrainAsync_WithNoData_ReturnsBadRequest()
    {
        using var db = CreateContext(nameof(TrainAsync_WithNoData_ReturnsBadRequest));

        var result = await ClassifierGroupExtensions.TrainAsync(db);

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

        var result = await ClassifierGroupExtensions.TrainAsync(db);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<bool>>>();
        ok.Value!.IsSuccess.ShouldBeTrue();
        ok.Value.Value.ShouldBeTrue();
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

        var result = await ClassifierGroupExtensions.TrainAsync(db);

        var badRequest = result.ShouldBeOfType<BadRequest<ApiResponse<bool>>>();
        badRequest.Value!.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task PredictAsync_WithUntrainedClassifier_ReturnsEmptyResult()
    {
        using var db = CreateContext(nameof(PredictAsync_WithUntrainedClassifier_ReturnsEmptyResult));
        var request = new PredictRequest("test-id");

        var result = await ClassifierGroupExtensions.PredictAsync(request, db);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<PredictionResult>>>();
        ok.Value!.IsSuccess.ShouldBeTrue();
        ok.Value.Value!.PredictedBucket.ShouldBe("");
        ok.Value.Value.Confidence.ShouldBe(0);
    }

    [Fact]
    public async Task PredictAsync_WithNonExistentEmail_ReturnsNotFound()
    {
        using var db = CreateContext(nameof(PredictAsync_WithNonExistentEmail_ReturnsNotFound));
        // Train first to set the static classifier
        db.Emails.Add(CreateEmail("train-1", "Inbox", "Work"));
        await db.SaveChangesAsync();
        await ClassifierGroupExtensions.TrainAsync(db);

        var request = new PredictRequest("non-existent");
        var result = await ClassifierGroupExtensions.PredictAsync(request, db);

        var notFound = result.ShouldBeOfType<NotFound<ApiResponse<PredictionResult>>>();
        notFound.Value!.IsSuccess.ShouldBeFalse();
        notFound.Value.Error!.Code.ShouldBe("EMAIL_NOT_FOUND");
    }

    [Fact]
    public async Task PredictAsync_WithTrainedClassifier_AndExistingEmail_ReturnsPrediction()
    {
        using var db = CreateContext(nameof(PredictAsync_WithTrainedClassifier_AndExistingEmail_ReturnsPrediction));
        db.Emails.Add(CreateEmail("train-1", "Inbox", "Work"));
        await db.SaveChangesAsync();
        await ClassifierGroupExtensions.TrainAsync(db);

        db.Emails.Add(CreateEmail("predict-1", "Inbox", "Work"));
        await db.SaveChangesAsync();

        var request = new PredictRequest("predict-1");
        var result = await ClassifierGroupExtensions.PredictAsync(request, db);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<PredictionResult>>>();
        ok.Value!.IsSuccess.ShouldBeTrue();
        ok.Value.Value.ShouldNotBeNull();
    }
}
