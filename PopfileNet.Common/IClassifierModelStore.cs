namespace PopfileNet.Common;

/// <summary>
/// Provides persistence for classifier models, storing model bytes on disk and metadata in a database.
/// </summary>
public interface IClassifierModelStore
{
    /// <summary>
    /// Determines whether a persisted model exists for the given owner.
    /// </summary>
    Task<bool> ExistsAsync(string ownerId, CancellationToken ct = default);

    /// <summary>
    /// Gets metadata for the persisted model of the given owner, or null when no model exists.
    /// </summary>
    Task<ClassifierModelMeta?> GetMetaAsync(string ownerId, CancellationToken ct = default);

    /// <summary>
    /// Returns the persisted model bytes for the given owner, or null when no model exists.
    /// </summary>
    Task<Stream?> OpenReadAsync(string ownerId, CancellationToken ct = default);

    /// <summary>
    /// Saves a model artifact and its metadata for the given owner.
    /// </summary>
    Task SaveAsync(string ownerId, Stream model, ClassifierModelMeta meta, CancellationToken ct = default);

    /// <summary>
    /// Deletes the persisted model and metadata for the given owner.
    /// </summary>
    Task DeleteAsync(string ownerId, CancellationToken ct = default);
}