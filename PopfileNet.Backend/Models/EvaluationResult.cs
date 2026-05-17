namespace PopfileNet.Backend.Models;

public record EvaluationResult(
    int NumberOfRuns,
    List<RunResultDto> Runs,
    AggregatedMetricsDto? Aggregated);

public record RunResultDto(
    int RunNumber,
    int TrainingCount,
    int TestCount,
    float Accuracy,
    int Correct,
    int Total,
    List<BucketMetricDto> BucketMetrics,
    List<MismatchDetailDto> Mismatches);

public record AggregatedMetricsDto(
    float MeanAccuracy,
    float MinAccuracy,
    float MaxAccuracy,
    Dictionary<string, AggregatedBucketMetricDto>? PerBucket);

public record BucketMetricDto(
    string BucketName,
    int TruePositives,
    int FalsePositives,
    int FalseNegatives,
    float Precision,
    float Recall);

public record AggregatedBucketMetricDto(
    float MeanPrecision,
    float MeanRecall);

public record MismatchDetailDto(
    string EmailId,
    string Subject,
    string ActualBucket,
    string PredictedBucket);
