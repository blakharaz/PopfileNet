namespace PopfileNet.Backend.Models;

public record EvaluationRequest(
    string FolderFilter = "all",
    string BucketFilter = "all",
    string CutoffType = "date",
    string? CutoffValue = null,
    float TrainTestSplit = 0.8f,
    int NumberOfRuns = 1);
