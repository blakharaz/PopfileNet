namespace PopfileNet.Backend.Models;

public record EvaluationResult(
    int NumberOfRuns,
    List<RunResultDto> Runs,
    AggregatedMetricsDto? Aggregated);
