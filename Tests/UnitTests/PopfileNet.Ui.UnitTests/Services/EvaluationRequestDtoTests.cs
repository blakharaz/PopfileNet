using PopfileNet.Ui.Services;
using Shouldly;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Services;

public sealed class EvaluationRequestDtoTests
{
    [Fact]
    public void EvaluationRequest_Defaults_AreCorrect()
    {
        var req = new EvaluationRequest();

        req.FolderFilter.ShouldBe("all");
        req.BucketFilter.ShouldBe("all");
        req.CutoffType.ShouldBe("date");
        req.CutoffValue.ShouldBeNull();
        req.TrainTestSplit.ShouldBe(0.8f);
        req.NumberOfRuns.ShouldBe(1);
    }

    [Fact]
    public void EvaluationRequest_CanSetProperties()
    {
        var req = new EvaluationRequest
        {
            FolderFilter = "Inbox",
            BucketFilter = "Work",
            CutoffType = "amount",
            CutoffValue = "100",
            TrainTestSplit = 0.7f,
            NumberOfRuns = 3
        };

        req.FolderFilter.ShouldBe("Inbox");
        req.BucketFilter.ShouldBe("Work");
        req.CutoffType.ShouldBe("amount");
        req.CutoffValue.ShouldBe("100");
        req.TrainTestSplit.ShouldBe(0.7f);
        req.NumberOfRuns.ShouldBe(3);
    }

    [Fact]
    public void EvaluationResult_DefaultValues()
    {
        var result = new EvaluationResult();

        result.NumberOfRuns.ShouldBe(0);
        result.Runs.ShouldNotBeNull();
        result.Runs.ShouldBeEmpty();
        result.Aggregated.ShouldBeNull();
    }

    [Fact]
    public void EvaluationResult_WithValues()
    {
        var run = new RunResultDto(1, 10, 5, 0.8f, 4, 5, [], []);
        var agg = new AggregatedMetricsDto(0.8f, 0.8f, 0.8f, null);
        var result = new EvaluationResult
        {
            NumberOfRuns = 1,
            Runs = [run],
            Aggregated = agg
        };

        result.NumberOfRuns.ShouldBe(1);
        result.Runs.Count.ShouldBe(1);
        result.Aggregated.ShouldNotBeNull();
        result.Aggregated.MeanAccuracy.ShouldBe(0.8f);
    }

    [Fact]
    public void MismatchDetailDto_StoresValues()
    {
        var dto = new MismatchDetailDto("email-1", "Test subject", "Work", "Personal");

        dto.EmailId.ShouldBe("email-1");
        dto.Subject.ShouldBe("Test subject");
        dto.ActualBucket.ShouldBe("Work");
        dto.PredictedBucket.ShouldBe("Personal");
    }

    [Fact]
    public void BucketMetricDto_StoresValues()
    {
        var dto = new BucketMetricDto("Work", 10, 2, 1, 0.83f, 0.91f);

        dto.BucketName.ShouldBe("Work");
        dto.TruePositives.ShouldBe(10);
        dto.FalsePositives.ShouldBe(2);
        dto.FalseNegatives.ShouldBe(1);
        dto.Precision.ShouldBe(0.83f);
        dto.Recall.ShouldBe(0.91f);
    }

    [Fact]
    public void AggregatedMetricsDto_Default_NullPerBucket()
    {
        var dto = new AggregatedMetricsDto(0.5f, 0.4f, 0.6f, null);

        dto.MeanAccuracy.ShouldBe(0.5f);
        dto.MinAccuracy.ShouldBe(0.4f);
        dto.MaxAccuracy.ShouldBe(0.6f);
        dto.PerBucket.ShouldBeNull();
    }

    [Fact]
    public void AggregatedBucketMetricDto_StoresValues()
    {
        var dto = new AggregatedBucketMetricDto(0.83f, 0.91f);

        dto.MeanPrecision.ShouldBe(0.83f);
        dto.MeanRecall.ShouldBe(0.91f);
    }

    [Fact]
    public void BucketInfoDto_StoresValues()
    {
        var dto = new BucketInfoDto("bucket-1", "Work");

        dto.Id.ShouldBe("bucket-1");
        dto.Name.ShouldBe("Work");
    }
}
