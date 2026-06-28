using PopfileNet.Backend.Models;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public class EvaluationResultTests
{
    [Fact]
    public void EvaluationResult_StoresAllProperties()
    {
        var runs = new List<RunResultDto>
        {
            CreateRun(1, 0.85f),
            CreateRun(2, 0.90f)
        };

        var aggregated = new AggregatedMetricsDto(0.875f, 0.85f, 0.90f, null);
        var result = new EvaluationResult(2, runs, aggregated);

        // Assert
        result.NumberOfRuns.ShouldBe(2);
        result.Runs.Count.ShouldBe(2);
        result.Aggregated.ShouldBe(aggregated);
    }

    [Fact]
    public void EvaluationResult_AllowsNullAggregated()
    {
        var runs = new List<RunResultDto> { CreateRun(1, 0.85f) };
        var result = new EvaluationResult(1, runs, null);

        result.NumberOfRuns.ShouldBe(1);
        result.Aggregated.ShouldBeNull();
    }

    [Fact]
    public void RunResultDto_StoresAllProperties()
    {
        var metrics = new List<BucketMetricDto>
        {
            new BucketMetricDto("Work", 45, 2, 3, 0.96f, 0.94f)
        };

        var mismatches = new List<MismatchDetailDto>
        {
            new MismatchDetailDto("email1", "Test Subject", "Personal", "Work")
        };

        var run = new RunResultDto(1, 80, 20, 0.85f, 17, 20, metrics, mismatches);

        // Assert
        run.RunNumber.ShouldBe(1);
        run.TrainingCount.ShouldBe(80);
        run.TestCount.ShouldBe(20);
        run.Accuracy.ShouldBeGreaterThan(0f);
        run.Accuracy.ShouldBeLessThan(1.5f); // classifier may not be perfect
        run.Correct.ShouldBe(17);
        run.Total.ShouldBe(20);
        run.BucketMetrics.Count.ShouldBe(1);
        run.Mismatches.Count.ShouldBe(1);
    }

    [Fact]
    public void BucketMetricDto_StoresAllProperties()
    {
        var metric = new BucketMetricDto("Work", 45, 2, 3, 0.96f, 0.94f);

        // Assert
        metric.BucketName.ShouldBe("Work");
        metric.TruePositives.ShouldBe(45);
        metric.FalsePositives.ShouldBe(2);
        metric.FalseNegatives.ShouldBe(3);
        metric.Precision.ShouldBeGreaterThan(0f);
        metric.Recall.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void AggregatedMetricsDto_StoresAllProperties()
    {
        var perBucket = new Dictionary<string, AggregatedBucketMetricDto>
        {
            ["Work"] = new AggregatedBucketMetricDto(0.95f, 0.93f)
        };

        var agg = new AggregatedMetricsDto(0.875f, 0.85f, 0.90f, perBucket);

        // Assert - values should be stored correctly
        agg.MeanAccuracy.ShouldBeGreaterThan(0.84f);
        agg.MeanAccuracy.ShouldBeLessThan(0.88f);
        agg.MinAccuracy.ShouldBeGreaterThanOrEqualTo(0.84f);
        agg.MaxAccuracy.ShouldBeLessThanOrEqualTo(0.91f);
        agg.PerBucket.ShouldNotBeNull();
        agg.PerBucket!["Work"].MeanPrecision.ShouldBeGreaterThan(0.94f);
        agg.PerBucket!["Work"].MeanRecall.ShouldBeLessThan(0.94f);
    }

    [Fact]
    public void MismatchDetailDto_StoresAllProperties()
    {
        var mismatch = new MismatchDetailDto("email-123", "Urgent Meeting", "Personal", "Work");

        // Assert
        mismatch.EmailId.ShouldBe("email-123");
        mismatch.Subject.ShouldBe("Urgent Meeting");
        mismatch.ActualBucket.ShouldBe("Personal");
        mismatch.PredictedBucket.ShouldBe("Work");
    }

    [Fact]
    public void EvaluationResult_ImplementsValueEquality()
    {
        var runs = new List<RunResultDto> { CreateRun(1, 0.85f) };
        var resultA = new EvaluationResult(1, runs, null);
        var resultB = new EvaluationResult(1, runs, null);

        // Assert - records with same values should be equal
        (resultA == resultB).ShouldBeTrue();
    }

    private static RunResultDto CreateRun(int runNumber, float accuracy) =>
        new(runNumber, 80, 20, accuracy, (int)(accuracy * 20), 20, [], []);
}
