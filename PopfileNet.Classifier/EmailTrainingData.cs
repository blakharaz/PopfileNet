using Microsoft.ML.Data;

namespace PopfileNet.Classifier;

public class EmailTrainingData {
    [LoadColumn(0)]
    public string Content { get; set; } = default!;

    [LoadColumn(1)]
    public string Category { get; set; } = default!;
}
