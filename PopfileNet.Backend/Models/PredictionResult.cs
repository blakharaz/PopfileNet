namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents the result of an email classification prediction.
/// </summary>
public record PredictionResult(string PredictedBucket, float Confidence, Dictionary<string, float> AllProbabilities);
