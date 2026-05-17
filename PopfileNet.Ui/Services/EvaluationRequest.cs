using System.Text.Json.Serialization;

namespace PopfileNet.Ui.Services;

public record MismatchDetailDto(
    string EmailId,
    string Subject,
    string ActualBucket,
    string PredictedBucket);

public class EvaluationRequest
{
    public string FolderFilter { get; set; } = "all";
    public string BucketFilter { get; set; } = "all";
    public string CutoffType { get; set; } = "date";
    public string? CutoffValue { get; set; }
    public float TrainTestSplit { get; set; } = 0.8f;
    public int NumberOfRuns { get; set; } = 1;
}

public class EvaluationResult
{
    [JsonPropertyName("NumberOfRuns")]
    public int NumberOfRuns { get; init; } = 0;
    
    [JsonPropertyName("Runs")]
    public List<RunResultDto> Runs { get; init; } = new();
    
    [JsonPropertyName("Aggregated")]
    public AggregatedMetricsDto? Aggregated { get; init; }
}

public record RunResultDto(
    int RunNumber, 
    int TrainingCount, 
    int TestCount, 
    float Accuracy, 
    int Correct, 
    int Total, 
    List<BucketMetricDto> BucketMetrics, 
    List<MismatchDetailDto> Mismatches);

public class AggregatedMetricsDto(
    float MeanAccuracy,
    float MinAccuracy,
    float MaxAccuracy,
    Dictionary<string, AggregatedBucketMetricDto>? PerBucket)
{
    public float MeanAccuracy { get; } = MeanAccuracy;
    public float MinAccuracy { get; } = MinAccuracy;
    public float MaxAccuracy { get; } = MaxAccuracy;
    public Dictionary<string, AggregatedBucketMetricDto>? PerBucket { get; init; } = PerBucket;
}

public record BucketMetricDto(
    string BucketName,
    int TruePositives,
    int FalsePositives,
    int FalseNegatives,
    float Precision,
    float Recall);

public record AggregatedBucketMetricDto(float MeanPrecision, float MeanRecall)
{
    public float MeanPrecision { get; } = MeanPrecision;
    public float MeanRecall { get; } = MeanRecall;
}
