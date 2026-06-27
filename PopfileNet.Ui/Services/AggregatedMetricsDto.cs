namespace PopfileNet.Ui.Services;

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
