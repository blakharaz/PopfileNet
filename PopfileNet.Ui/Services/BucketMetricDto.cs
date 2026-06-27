namespace PopfileNet.Ui.Services;

public record BucketMetricDto(
    string BucketName,
    int TruePositives,
    int FalsePositives,
    int FalseNegatives,
    float Precision,
    float Recall);
