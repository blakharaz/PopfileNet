namespace PopfileNet.Backend.Services;

/// <summary>
/// Configuration for the classifier model store and in-memory cache.
/// </summary>
public record ClassifierOptions
{
    /// <summary>
    /// Gets or sets the root directory where model artifacts are persisted.
    /// </summary>
    public string ModelsRoot { get; init; } = "classifier-models";

    /// <summary>
    /// Gets or sets the maximum number of classifier instances kept in the in-memory cache.
    /// </summary>
    public int MaxCachedModels { get; init; } = 16;

    /// <summary>
    /// Gets or sets how long an idle classifier instance may stay in the cache.
    /// </summary>
    public TimeSpan CacheTtl { get; init; } = TimeSpan.FromMinutes(20);
}