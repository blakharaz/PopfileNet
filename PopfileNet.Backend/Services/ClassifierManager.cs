using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using PopfileNet.Backend.Models;
using PopfileNet.Classifier;
using PopfileNet.Common;

namespace PopfileNet.Backend.Services;

/// <summary>
/// Provides per-owner classifier instances, loading models from the store on demand and caching them in memory.
/// </summary>
public class ClassifierManager(
    IClassifierModelStore store,
    IOptions<ClassifierOptions> options,
    Func<DateTime>? utcNow = null,
    ILogger<ClassifierManager>? logger = null)
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly Func<DateTime> _utcNow = utcNow ?? (() => DateTime.UtcNow);

    /// <summary>
    /// Gets the number of classifier instances currently held in the in-memory cache.
    /// </summary>
    internal int CacheCount => _cache.Count;

    /// <summary>
    /// Returns a trained classifier for the given owner, loading the persisted model on demand.
    /// Returns null when no model has been persisted for the owner.
    /// </summary>
    /// <remarks>
    /// Corrupted or missing artifacts are discarded together with their metadata so the owner is
    /// reported as not trained and can retrain.
    /// </remarks>
    public async Task<NaiveBayesianClassifier?> GetModelAsync(string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        if (_cache.TryGetValue(ownerId, out var entry))
        {
            entry.LastUsedTicks = _utcNow().Ticks;
            return entry.Model;
        }

        var meta = await store.GetMetaAsync(ownerId, ct);
        if (meta == null)
            return null;

        await using var stream = await store.OpenReadAsync(ownerId, ct);
        if (stream == null)
        {
            logger?.LogWarning(
                "Classifier metadata for owner {OwnerId} exists but the model artifact is missing. Removing stale metadata.",
                ownerId);
            await store.DeleteAsync(ownerId, ct);
            return null;
        }

        var classifier = new NaiveBayesianClassifier();
        try
        {
            classifier.Load(stream, meta.TrainingSampleCount);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or FormatException or InvalidDataException)
        {
            logger?.LogWarning(ex, "Discarding corrupted classifier model for owner {OwnerId}.", ownerId);
            _cache.TryRemove(ownerId, out _);
            await store.DeleteAsync(ownerId, ct);
            return null;
        }

        _cache[ownerId] = new CacheEntry(classifier, ClassifierSource.LoadedFromDisk) { LastUsedTicks = _utcNow().Ticks };
        Evict();

        return classifier;
    }

    /// <summary>
    /// Persists the given classifier for the owner and caches it for subsequent requests.
    /// </summary>
    public async Task SaveModelAsync(string ownerId, NaiveBayesianClassifier classifier, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(classifier);

        if (!classifier.IsTrained)
            throw new InvalidOperationException("Cannot persist an untrained classifier. Call Train() first.");

        var meta = new ClassifierModelMeta
        {
            OwnerId = ownerId,
            TrainingSampleCount = classifier.TrainingSampleCount,
            TrainedAtUtc = _utcNow()
        };

        await using var stream = new MemoryStream();
        classifier.Save(stream);
        stream.Position = 0;

        await store.SaveAsync(ownerId, stream, meta, ct);

        _cache[ownerId] = new CacheEntry(classifier, ClassifierSource.TrainedInMemory) { LastUsedTicks = _utcNow().Ticks };
        Evict();
    }

    /// <summary>
    /// Removes the cached classifier instance for the given owner, forcing a reload from the store on next use.
    /// </summary>
    public void Invalidate(string ownerId)
    {
        if (!string.IsNullOrWhiteSpace(ownerId))
            _cache.TryRemove(ownerId, out _);
    }

    /// <summary>
    /// Returns metadata about the persisted model for the given owner, or null when none exists.
    /// </summary>
    public Task<ClassifierModelMeta?> GetMetaAsync(string ownerId, CancellationToken ct = default)
        => store.GetMetaAsync(ownerId, ct);

    /// <summary>
    /// Returns the current status for the given owner, including where the model comes from.
    /// </summary>
    /// <remarks>
    /// A model only counts as trained when both its metadata and its artifact are present on disk.
    /// </remarks>
    public async Task<ClassifierStatus> GetStatusAsync(string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        if (!await store.ExistsAsync(ownerId, ct))
            return new ClassifierStatus(false, 0);

        var meta = await store.GetMetaAsync(ownerId, ct);
        if (meta == null)
            return new ClassifierStatus(false, 0);

        var source = _cache.TryGetValue(ownerId, out var entry)
            ? entry.Origin
            : ClassifierSource.LoadedFromDisk;

        return new ClassifierStatus(true, meta.TrainingSampleCount, source, meta.TrainedAtUtc);
    }

    /// <summary>
    /// Evicts idle entries past the cache TTL and, when over <see cref="ClassifierOptions.MaxCachedModels"/>, the least-recently-used entries.
    /// </summary>
    internal void Evict()
    {
        var now = _utcNow().Ticks;
        var ttlTicks = options.Value.CacheTtl.Ticks;
        var capacity = options.Value.MaxCachedModels;

        if (ttlTicks >= 0)
        {
            foreach (var (owner, entry) in _cache)
            {
                if (now - entry.LastUsedTicks >= ttlTicks)
                    _cache.TryRemove(owner, out _);
            }
        }

        if (_cache.Count <= capacity)
            return;

        // Evict least-recently-used entries when over capacity.
        var victims = _cache
            .OrderBy(kv => kv.Value.LastUsedTicks)
            .Take(_cache.Count - capacity)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var owner in victims)
            _cache.TryRemove(owner, out _);
    }

    private sealed class CacheEntry(NaiveBayesianClassifier model, string origin)
    {
        public NaiveBayesianClassifier Model { get; } = model;
        public string Origin { get; } = origin;
        public long LastUsedTicks { get; set; }
    }
}
