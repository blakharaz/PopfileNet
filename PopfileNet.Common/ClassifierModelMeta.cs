namespace PopfileNet.Common;

/// <summary>
/// Describes a persisted classifier model for a specific owner.
/// </summary>
public class ClassifierModelMeta
{
    public const int CurrentFormatVersion = 1;

    /// <summary>
    /// Gets or sets the owner (user/tenant) that the model belongs to.
    /// </summary>
    public string OwnerId { get; set; } = "";

    /// <summary>
    /// Gets or sets the number of training samples used to train the model.
    /// </summary>
    public int TrainingSampleCount { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the model was persisted.
    /// </summary>
    public DateTime TrainedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the format version of the persisted model artifact.
    /// </summary>
    public int FormatVersion { get; set; } = CurrentFormatVersion;
}