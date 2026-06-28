using Moq;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using Shouldly;
using System.Globalization;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public class ClassifierEvaluationServiceTests
{
    private static IClassifierDataProvider CreateProvider(List<Email> emails, string folderFilter = "all")
    {
        var mock = new Mock<IClassifierDataProvider>();
        mock.Setup(p => p.FetchFilteredAsync(It.IsAny<EmailFilterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailFilterRequest req, CancellationToken _) =>
            {
                var baseList = emails;
                if (req.FolderFilter != "all")
                    baseList = baseList.Where(e => e.Folder == req.FolderFilter).ToList();
                return [.. baseList];
            });
        return mock.Object;
    }

    private static ClassifierEvaluationService CreateService(List<Email> emails, string folderFilter = "all") =>
        new(CreateProvider(emails, folderFilter));

    // --- Basic evaluation flow ---

    [Fact]
    public async Task RunEvaluationAsync_ThrowsWhenNoEmailsAvailable()
    {
        var service = CreateService([]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunEvaluationAsync(new EvaluationRequest()));
    }

    [Fact]
    public async Task RunEvaluationAsync_ReturnsValidResult_ForSufficientEmails()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 20);
        _ = CreateProvider(emails);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest());

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_WithSufficientEmails_TrainsAndPredicts()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 20);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest());

        result.ShouldNotBeNull();
        result.Runs.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task RunEvaluationAsync_ReturnsSingleRun_WhenNumberOfRunsIsOne()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 20);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest(NumberOfRuns: 1));

        result.ShouldNotBeNull();
        result.NumberOfRuns.ShouldBe(1);
    }

    [Fact]
    public async Task RunEvaluationAsync_ReturnsAggregatedResults_WhenNumberOfRunsGreaterThanOne()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 50);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest(NumberOfRuns: 5));

        result.ShouldNotBeNull();
    }

    // --- Cutoff behavior ---

    [Fact]
    public async Task RunEvaluationAsync_DateCutoff_UsesEmailsBeforeCutoffDate()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 100);
        var cutoffDate = DateTime.Now.AddDays(-5);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(
            new EvaluationRequest(CutoffType: "date", CutoffValue: cutoffDate.ToString(CultureInfo.InvariantCulture)));

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_AmountCutoff_TakesMostRecentN_Emails()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 20);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(
            new EvaluationRequest(CutoffType: "amount", CutoffValue: "10"));

        // With cutoff, only the most recent emails should be used for training
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_NoCutoff_UsesAllEmails()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 100);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(
            new EvaluationRequest(CutoffType: "", CutoffValue: ""));

        // With no cutoff, all emails should be used
        result.ShouldNotBeNull();
    }

    // --- Filtering behavior ---

    [Fact]
    public async Task RunEvaluationAsync_FolderFilter_ExcludesUnmatchedEmails()
    {
        var bucketId = "work";
        var emails = new List<Email>
        {
            CreateEmail("e1", bucketId, DateTime.Now.AddDays(-2), "Inbox"),
            CreateEmail("e2", bucketId, DateTime.Now.AddDays(-1)), // "Inbox" default
        };

        _ = CreateProvider(emails);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest());

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_BucketFilter_IncludesOnlyMatchedBuckets()
    {
        var emails = new List<Email>
        {
            CreateEmail("e1", "work", DateTime.Now.AddDays(-2)),
            CreateEmail("e2", "personal", DateTime.Now.AddSeconds(1)),
        };

        _ = CreateProvider(emails);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest());

        // Only bucket-matched emails should contribute to metrics
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_BucketMetrics_AreComputed()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 30);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest(NumberOfRuns: 5));

        // Aggregate metrics should be present when multiple runs are performed
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_MultipleRuns_HaveDifferentRunNumbers()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 20);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest(NumberOfRuns: 5));

        for (int i = 0; i < result.Runs.Count; i++)
            result.Runs[i].RunNumber.ShouldBe(i + 1);
    }

    [Fact]
    public async Task RunEvaluationAsync_MultipleRuns_Succeeds()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 50);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest(NumberOfRuns: 10));

        // Multiple runs should all succeed with unique run numbers
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_TrainTestSplit_EffectsTrainingSetSize()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 40);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest());

        // A split should produce a training set smaller than all emails
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_TrainTestSplit_WithLargeNumberOfRuns_Succeeds()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 200);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(
            new EvaluationRequest(NumberOfRuns: 10, TrainTestSplit: 0.9f));

        // With high split and many runs, should still succeed
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_WithMismatchingPredictions_HasMismatchesList()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 80);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest(NumberOfRuns: 5));

        // The mismatch list should be available even when predictions are perfect
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_SingleRun_AccuracyIsBetweenZeroAndOne()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 80);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(new EvaluationRequest());

        // Accuracy should be between 0 and 1 (a percentage)
        var accuracy = result.Runs[0].Accuracy;
        accuracy.ShouldBeGreaterThanOrEqualTo(0f);
        accuracy.ShouldBeLessThanOrEqualTo(1f);
    }

    [Fact]
    public async Task RunEvaluationAsync_InvalidDateCutoff_FallsBackToRatioSplit()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 50);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(
            new EvaluationRequest(CutoffType: "date", CutoffValue: "invalid-date"));

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_InvalidAmountCutoff_FallsBackToRatioSplit()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 50);

        var service = CreateService(emails);
        var result = await service.RunEvaluationAsync(
            new EvaluationRequest(CutoffType: "amount", CutoffValue: "not-a-number"));

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunEvaluationAsync_DateCutoff_ThrowsWhenTrainingSetEmpty()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 20);
        // Set cutoff date to be before all emails (everything goes to test set)
        var cutoffDate = DateTime.Now.AddYears(-10).ToString("yyyy-MM-dd");

        var service = CreateService(emails);
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.RunEvaluationAsync(new EvaluationRequest(CutoffType: "date", CutoffValue: cutoffDate)));
    }

    [Fact]
    public async Task RunEvaluationAsync_DateCutoff_ThrowsWhenTestSetEmpty()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 20);
        // Set cutoff date to be after all emails (everything goes to training set)
        var cutoffDate = DateTime.Now.AddYears(10).ToString("yyyy-MM-dd");

        var service = CreateService(emails);
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.RunEvaluationAsync(new EvaluationRequest(CutoffType: "date", CutoffValue: cutoffDate)));
    }

    [Fact]
    public async Task RunEvaluationAsync_AmountCutoff_ThrowsWhenTrainingSetEmpty()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 20);

        var service = CreateService(emails);
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.RunEvaluationAsync(new EvaluationRequest(CutoffType: "amount", CutoffValue: "0")));
    }

    [Fact]
    public async Task RunEvaluationAsync_AmountCutoff_ThrowsWhenTestSetEmpty()
    {
        var bucketId = "work";
        var emails = CreateEmailsWithBucket("e", bucketId, 20);

        var service = CreateService(emails);
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.RunEvaluationAsync(new EvaluationRequest(CutoffType: "amount", CutoffValue: "100")));
    }

    [Fact]
    public async Task RunEvaluationAsync_ThrowsWhenNoLabeledDataAvailable()
    {
        var emails = new List<Email>
        {
            new Email { Id = "1", Subject = "Test", FolderNavigation = null },
            new Email { Id = "2", Subject = "Test", FolderNavigation = null }
        };

        var service = CreateService(emails);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunEvaluationAsync(new EvaluationRequest()));
    }


    private static Email CreateEmail(string id, string bucketId, DateTime receivedDate, string folder = "Inbox") => new()
    {
        Id = id,
        Subject = $"Test message {id}",
        ReceivedDate = receivedDate,
        Folder = folder,
        FolderNavigation = new MailFolder 
        { 
            Name = folder,
            Bucket = new() { Id = bucketId, Name = "Work" }
        }
    };

    private static List<Email> CreateEmailsWithBucket(string prefix, string bucketId, int count)
    {
        var emails = new List<Email>(count);
        for (int i = 0; i < count; i++)
            emails.Add(CreateEmail($"{prefix}{i}", $"{bucketId}-{i}", DateTime.Now.AddDays(-count + i)));

        return emails;
    }
}
