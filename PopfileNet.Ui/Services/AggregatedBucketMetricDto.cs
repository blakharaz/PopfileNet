namespace PopfileNet.Ui.Services;

public record AggregatedBucketMetricDto(float MeanPrecision, float MeanRecall)
{
    public float MeanPrecision { get; } = MeanPrecision;
    public float MeanRecall { get; } = MeanRecall;
}
