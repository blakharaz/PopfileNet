namespace PopfileNet.Backend.Models;

/// <summary>
/// Describes where the classifier model for an owner currently comes from.
/// </summary>
public static class ClassifierSource
{
    /// <summary>
    /// No model has been persisted for the owner.
    /// </summary>
    public const string NotTrained = "NotTrained";

    /// <summary>
    /// The model was loaded from a persisted artifact written by a previous process.
    /// </summary>
    public const string LoadedFromDisk = "LoadedFromDisk";

    /// <summary>
    /// The model was trained during the current process (and persisted).
    /// </summary>
    public const string TrainedInMemory = "TrainedInMemory";
}
