namespace PopfileNet.Ui.Services;

public class EvaluationRequest
{
    public string FolderFilter { get; set; } = "all";
    public string BucketFilter { get; set; } = "all";
    public string CutoffType { get; set; } = "date";
    public string? CutoffValue { get; set; }
    public float TrainTestSplit { get; set; } = 0.8f;
    public int NumberOfRuns { get; set; } = 1;
}
