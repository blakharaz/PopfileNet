namespace PopfileNet.Backend.Models;

public record BucketMetricDto(
    string BucketName,
    int TruePositives,
    int FalsePositives,
    int FalseNegatives,
    float Precision,
    float Recall);
