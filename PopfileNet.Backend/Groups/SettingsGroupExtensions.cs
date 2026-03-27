using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

/// <summary>
/// Provides API endpoints for application settings.
/// </summary>
public static class SettingsGroupExtensions
{
    /// <summary>
    /// Maps the settings endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication AddSettingsGroup(this WebApplication app)
    {
        var group = app.MapGroup("/settings");
        
        group.MapGet("/", GetSettingsAsync);
        group.MapPost("/", SaveSettingsAsync);
        group.MapPost("/test-connection", TestConnectionAsync);
        
        group.MapGet("/buckets", GetBucketsAsync);
        group.MapPost("/buckets", CreateBucketAsync);
        group.MapPut("/buckets/{id}", UpdateBucketAsync);
        group.MapDelete("/buckets/{id}", DeleteBucketAsync);
        
        group.MapGet("/folder-mappings", GetFolderMappingsAsync);
        group.MapPost("/folder-mappings", SetFolderMappingAsync);
        group.MapDelete("/folder-mappings/{folderName}", RemoveFolderMappingAsync);

        return app;
    }

    private static async Task<Ok<ApiResponse<AppSettings>>> GetSettingsAsync(ISettingsService settingsService, ILogger<Program> logger)
    {
        try
        {
            var settings = await settingsService.GetSettingsAsync();
            return TypedResults.Ok(ApiResponse<AppSettings>.Success(settings));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting settings");
            throw;
        }
    }

    private static async Task<IResult> SaveSettingsAsync(AppSettings settings, ISettingsService settingsService)
    {
        await settingsService.SaveSettingsAsync(settings);
        return TypedResults.Ok(ApiResponse<bool>.Success(true));
    }

    internal static async Task<IResult> TestConnectionAsync(IImapService imapClient)
    {
        if (!await imapClient.IsConfiguredAsync())
        {
            // settings are missing – inform caller instead of throwing
            return TypedResults.BadRequest(ApiResponse<bool>.Failure("IMAP_NOT_CONFIGURED", "IMAP settings are not configured"));
        }

        var result = await imapClient.TestConnectionAsync();
        return TypedResults.Ok(ApiResponse<bool>.Success(result));
    }

    private static async Task<Ok<PagedApiResponse<BucketDto>>> GetBucketsAsync(PopfileNetDbContext db, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        
        var totalCount = await db.Buckets.CountAsync();
        var buckets = await db.Buckets
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BucketDto(b.Id, b.Name, b.Description ?? ""))
            .ToListAsync();
        
        return TypedResults.Ok(PagedApiResponse<BucketDto>.Success(buckets, page, pageSize, totalCount));
    }

    private static async Task<IResult> CreateBucketAsync(BucketDto bucket, PopfileNetDbContext db)
    {
        var newBucket = new Bucket
        {
            Id = bucket.Id == string.Empty ? Guid.NewGuid().ToString() : bucket.Id,
            Name = bucket.Name,
            Description = bucket.Description
        };
        
        db.Buckets.Add(newBucket);
        await db.SaveChangesAsync();
        
        var result = new BucketDto(newBucket.Id, newBucket.Name, newBucket.Description);
        return TypedResults.Created($"/settings/buckets/{newBucket.Id}", ApiResponse<BucketDto>.Success(result));
    }

    private static async Task<IResult> UpdateBucketAsync(string id, BucketDto bucket, PopfileNetDbContext db)
    {
        var existing = await db.Buckets.FindAsync(id);
        if (existing == null)
        {
            return TypedResults.NotFound();
        }

        existing.Name = bucket.Name;
        existing.Description = bucket.Description;
        
        await db.SaveChangesAsync();
        
        return TypedResults.Ok(ApiResponse<BucketDto>.Success(new BucketDto(existing.Id, existing.Name, existing.Description)));
    }

    private static async Task<IResult> DeleteBucketAsync(string id, PopfileNetDbContext db)
    {
        var bucket = await db.Buckets.FindAsync(id);
        if (bucket == null)
        {
            return TypedResults.NotFound();
        }

        db.Buckets.Remove(bucket);
        await db.SaveChangesAsync();
        
        return Results.NoContent();
    }

    private static async Task<Ok<ApiResponse<IReadOnlyList<FolderMappingDto>>>> GetFolderMappingsAsync(ISettingsService settingsService)
    {
        var folderMappings = await settingsService.GetFolderMappingsAsync();
        return TypedResults.Ok(ApiResponse<IReadOnlyList<FolderMappingDto>>.Success(folderMappings));
    }

    private static async Task<IResult> SetFolderMappingAsync(FolderMappingDto mapping, ISettingsService settingsService)
    {
        try
        {
            await settingsService.SetFolderMappingAsync(mapping.Name, mapping.BucketId);
            return TypedResults.Ok(ApiResponse<FolderMappingDto>.Success(mapping));
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ApiResponse<FolderMappingDto>.Failure("INVALID_INPUT", ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            // Determine if it's a folder or bucket not found based on the message
            if (ex.Message.Contains("Folder", StringComparison.OrdinalIgnoreCase))
            {
                return TypedResults.NotFound(ApiResponse<FolderMappingDto>.Failure("NOT_FOUND", ex.Message));
            }
            else if (ex.Message.Contains("Bucket", StringComparison.OrdinalIgnoreCase))
            {
                return TypedResults.NotFound(ApiResponse<FolderMappingDto>.Failure("NOT_FOUND", ex.Message));
            }
            else
            {
                return TypedResults.BadRequest(ApiResponse<FolderMappingDto>.Failure("NOT_FOUND", ex.Message));
            }
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(ApiResponse<FolderMappingDto>.Failure("ERROR", ex.Message));
        }
    }

    private static async Task<IResult> RemoveFolderMappingAsync(string folderName, ISettingsService settingsService)
    {
        try
        {
            await settingsService.RemoveFolderMappingAsync(folderName);
            return TypedResults.NoContent();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ApiResponse<bool>.Failure("INVALID_INPUT", ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(ApiResponse<bool>.Failure("NOT_FOUND", ex.Message));
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(ApiResponse<bool>.Failure("ERROR", ex.Message));
        }
    }
}
