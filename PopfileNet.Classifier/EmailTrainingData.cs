using Microsoft.ML.Data;

namespace PopfileNet.Classifier;

public class EmailTrainingData {
    [LoadColumn(0)]
    public required string Subject { get; set; }

    [LoadColumn(1)]
    public required string Content { get; set; }

    [LoadColumn(2)]
    public required string Label { get; set; }
}
