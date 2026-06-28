namespace PopfileNet.Backend.Models;

public record AggregatedMetricsDto(
    float MeanAccuracy,
    float MinAccuracy,
    float MaxAccuracy,
    Dictionary<string, AggregatedBucketMetricDto>? PerBucket);
