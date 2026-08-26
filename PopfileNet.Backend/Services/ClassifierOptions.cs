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

    private int _maxCachedModels = 16;

    /// <summary>
    /// Gets or sets the maximum number of classifier instances kept in the in-memory cache.
    /// Values below 1 are clamped to 1.
    /// </summary>
    public int MaxCachedModels
    {
        get => _maxCachedModels;
        init => _maxCachedModels = Math.Max(1, value);
    }

    private TimeSpan _cacheTtl = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Gets or sets how long an idle classifier instance may stay in the cache.
    /// Negative values are clamped to <see cref="TimeSpan.Zero"/>, which evicts idle entries immediately.
    /// </summary>
    public TimeSpan CacheTtl
    {
        get => _cacheTtl;
        init => _cacheTtl = value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }
}