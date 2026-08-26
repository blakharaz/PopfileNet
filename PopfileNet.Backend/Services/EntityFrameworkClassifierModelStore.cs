using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Services;

/// <summary>
/// Persists classifier model artifacts on disk and their metadata in the database.
/// </summary>
public class EntityFrameworkClassifierModelStore(
    IDbContextFactory<PopfileNetDbContext> dbContextFactory,
    IOptions<ClassifierOptions> options) : IClassifierModelStore
{
    private const string ModelFileName = "model.zip";

    public async Task<bool> ExistsAsync(string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(ownerId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.ClassifierModels
            .AsNoTracking()
            .AnyAsync(m => m.OwnerId == ownerId, ct);
    }

    public async Task<ClassifierModelMeta?> GetMetaAsync(string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(ownerId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.ClassifierModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OwnerId == ownerId, ct);
    }

    public Task<Stream?> OpenReadAsync(string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(ownerId);

        var path = GetModelPath(ownerId);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public async Task SaveAsync(string ownerId, Stream model, ClassifierModelMeta meta, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(ownerId);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(meta);

        var path = GetModelPath(ownerId);

        // Write the blob first (temp file + rename for crash safety).
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tempPath = path + "." + RandomNumberGenerator.GetHexString(8);
        await using (var tempStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await model.CopyToAsync(tempStream, ct);
        }

        File.Move(tempPath, path, overwrite: true);

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            var existing = await dbContext.ClassifierModels.FindAsync([ownerId], ct);
            if (existing == null)
            {
                dbContext.ClassifierModels.Add(new ClassifierModelMeta
                {
                    OwnerId = ownerId,
                    TrainingSampleCount = meta.TrainingSampleCount,
                    TrainedAtUtc = meta.TrainedAtUtc,
                    FormatVersion = meta.FormatVersion
                });
            }
            else
            {
                existing.TrainingSampleCount = meta.TrainingSampleCount;
                existing.TrainedAtUtc = meta.TrainedAtUtc;
                existing.FormatVersion = meta.FormatVersion;
            }

            await dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            // Roll back the blob so the disk and metadata stay consistent.
            File.Delete(path);
            throw;
        }
    }

    public async Task DeleteAsync(string ownerId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(ownerId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var meta = await dbContext.ClassifierModels
            .FirstOrDefaultAsync(m => m.OwnerId == ownerId, ct);
        if (meta != null)
        {
            dbContext.ClassifierModels.Remove(meta);
            await dbContext.SaveChangesAsync(ct);
        }

        var path = GetModelPath(ownerId);
        if (File.Exists(path))
            File.Delete(path);
    }

    internal string GetModelPath(string ownerId)
    {
        var safeOwner = string.Concat(ownerId.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).ToLowerInvariant();
        return Path.Combine(options.Value.ModelsRoot, safeOwner, ModelFileName);
    }
}
