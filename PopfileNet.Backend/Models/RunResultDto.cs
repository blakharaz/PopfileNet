namespace PopfileNet.Backend.Models;

public record RunResultDto(
    int RunNumber,
    int TrainingCount,
    int TestCount,
    float Accuracy,
    int Correct,
    int Total,
    List<BucketMetricDto> BucketMetrics,
    List<MismatchDetailDto> Mismatches);
