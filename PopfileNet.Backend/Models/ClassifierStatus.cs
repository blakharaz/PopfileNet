namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents the status of the classifier for an owner.
/// </summary>
/// <param name="IsTrained">Whether a persisted model is available.</param>
/// <param name="TrainingDataCount">Number of samples the persisted model was trained on.</param>
/// <param name="Source">Where the model comes from, see <see cref="ClassifierSource"/>.</param>
/// <param name="TrainedAtUtc">When the persisted model was trained, if known.</param>
public record ClassifierStatus(
    bool IsTrained,
    int TrainingDataCount,
    string Source = ClassifierSource.NotTrained,
    DateTime? TrainedAtUtc = null);
