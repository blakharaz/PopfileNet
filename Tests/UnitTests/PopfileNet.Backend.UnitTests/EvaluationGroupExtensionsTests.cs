using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public sealed class EvaluationGroupExtensionsTests
{
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
            },
            ReceivedDate = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task RunEvaluationAsync_WithValidRequest_ReturnsOk()
    {
        var emails = Enumerable.Range(1, 30)
            .Select(i => CreateEmail($"e{i}", "Inbox", "Work"))
            .ToList();

        var mockDataProvider = new Mock<IClassifierDataProvider>();
        mockDataProvider.Setup(p => p.FetchFilteredAsync(
                It.IsAny<EmailFilterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        var service = new ClassifierEvaluationService(mockDataProvider.Object);
        var request = new EvaluationRequest(
            FolderFilter: "all",
            BucketFilter: "all",
            CutoffType: "amount",
            CutoffValue: "20",
            TrainTestSplit: 0.8f,
            NumberOfRuns: 1);

        var result = await EvaluationGroupExtensions.RunEvaluationAsync(
            request, service);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<EvaluationResult>>>();
        ok.Value.IsSuccess.ShouldBeTrue();
        ok.Value.Value.ShouldNotBeNull();
        ok.Value.Value.NumberOfRuns.ShouldBe(1);
    }

    [Fact]
    public async Task RunEvaluationAsync_WhenServiceThrows_ReturnsBadRequest()
    {
        var mockDataProvider = new Mock<IClassifierDataProvider>();
        mockDataProvider.Setup(p => p.FetchFilteredAsync(
                It.IsAny<EmailFilterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new ClassifierEvaluationService(mockDataProvider.Object);
        var request = new EvaluationRequest();

        var result = await EvaluationGroupExtensions.RunEvaluationAsync(
            request, service);

        var badRequest = result.ShouldBeOfType<BadRequest<ApiResponse<EvaluationResult>>>();
        badRequest.Value.IsSuccess.ShouldBeFalse();
        badRequest.Value.Error!.Code.ShouldBe("INVALID_CONFIG");
        badRequest.Value.Error.Message.ShouldContain("No emails available");
    }

    [Fact]
    public async Task RunEvaluationAsync_WithHighTrainTestSplit_ReturnsAggregated()
    {
        var emails = Enumerable.Range(1, 60)
            .Select(i => CreateEmail($"e{i}", "Inbox", "Work"))
            .ToList();

        var mockDataProvider = new Mock<IClassifierDataProvider>();
        mockDataProvider.Setup(p => p.FetchFilteredAsync(
                It.IsAny<EmailFilterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emails);

        var service = new ClassifierEvaluationService(mockDataProvider.Object);
        var request = new EvaluationRequest(
            FolderFilter: "all",
            BucketFilter: "all",
            CutoffType: "amount",
            CutoffValue: "50",
            TrainTestSplit: 0.8f,
            NumberOfRuns: 3);

        var result = await EvaluationGroupExtensions.RunEvaluationAsync(
            request, service);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<EvaluationResult>>>();
        ok.Value.IsSuccess.ShouldBeTrue();
        ok.Value.Value!.NumberOfRuns.ShouldBe(3);
    }

    [Fact]
    public async Task GetConfigAsync_ReturnsFoldersAndBuckets()
    {
        using var db = CreateContext(nameof(GetConfigAsync_ReturnsFoldersAndBuckets));
        db.MailFolders.AddRange(
            new MailFolder { Id = "f1", Name = "Inbox" },
            new MailFolder { Id = "f2", Name = "Sent" });
        db.Buckets.AddRange(
            new Bucket { Id = "b1", Name = "Work" },
            new Bucket { Id = "b2", Name = "Personal" });
        await db.SaveChangesAsync();

        var result = await EvaluationGroupExtensions.GetConfigAsync(db);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<object>>>();
        ok.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task GetConfigAsync_WithNoFolders_ReturnsEmptyFolders()
    {
        using var db = CreateContext(nameof(GetConfigAsync_WithNoFolders_ReturnsEmptyFolders));

        var result = await EvaluationGroupExtensions.GetConfigAsync(db);

        var ok = result.ShouldBeOfType<Ok<ApiResponse<object>>>();
        ok.Value.IsSuccess.ShouldBeTrue();
    }

    private static PopfileNetDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new PopfileNetDbContext(options);
    }
}
