using System.Text.Json.Serialization;

namespace PopfileNet.Ui.Services;

public class EvaluationResult
{
    [JsonPropertyName("NumberOfRuns")]
    public int NumberOfRuns { get; init; } = 0;
    
    [JsonPropertyName("Runs")]
    public List<RunResultDto> Runs { get; init; } = new();
    
    [JsonPropertyName("Aggregated")]
    public AggregatedMetricsDto? Aggregated { get; init; }
}
