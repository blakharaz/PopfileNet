namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents the status of the classifier.
/// </summary>
public record ClassifierStatus(bool IsTrained, int TrainingDataCount);
