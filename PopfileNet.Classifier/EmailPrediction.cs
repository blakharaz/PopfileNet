namespace PopfileNet.Classifier;

public class EmailPrediction {
    public float[] Scores { get; set; } = default!;
    public string PredictedLabel { get; set; } = default!;
}