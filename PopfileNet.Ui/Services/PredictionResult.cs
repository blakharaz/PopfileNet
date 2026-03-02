namespace PopfileNet.Ui.Services;

public record PredictionResult(string PredictedBucket, float Confidence, Dictionary<string, float> AllProbabilities)
{
    public static PredictionResult Empty => new("", 0, []);
}
