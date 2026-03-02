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

        return app;
    }

    private static async Task<Ok<ApiResponse<AppSettings>>> GetSettingsAsync(PopfileNetDbContext db)
    {
        try
        {
            var buckets = await db.Buckets.ToListAsync();
            var folders = await db.MailFolders.ToListAsync();
            
            var settings = new AppSettings
            {
                ImapSettings = new ImapSettingsDto(),
                Buckets = buckets.Select(b => new BucketDto(b.Id, b.Name, b.Description)).ToList(),
                FolderMappings = folders.Select(f => new FolderMappingDto(f.Name, f.Bucket?.Id)).ToList()
            };
            
            return TypedResults.Ok(ApiResponse<AppSettings>.Success(settings));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(ApiResponse<AppSettings>.Failure("SETTINGS_ERROR", ex.Message));
        }
    }

    private static async Task<IResult> SaveSettingsAsync(AppSettings settings, PopfileNetDbContext db, IConfiguration config)
    {
        try
        {
            config["ImapSettings:Server"] = settings.ImapSettings?.Server;
            config["ImapSettings:Port"] = settings.ImapSettings?.Port.ToString();
            config["ImapSettings:Username"] = settings.ImapSettings?.Username;
            config["ImapSettings:Password"] = settings.ImapSettings?.Password;
            config["ImapSettings:UseSsl"] = settings.ImapSettings?.UseSsl.ToString();

            return TypedResults.Ok(ApiResponse<bool>.Success(true));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(ApiResponse<bool>.Failure("SAVE_SETTINGS_ERROR", ex.Message));
        }
    }

    private static async Task<Ok<ApiResponse<bool>>> TestConnectionAsync(IImapService imapClient)
    {
        try
        {
            var result = await imapClient.TestConnectionAsync();
            return TypedResults.Ok(ApiResponse<bool>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(ApiResponse<bool>.Failure("CONNECTION_ERROR", ex.Message));
        }
    }

    private static async Task<Ok<PagedApiResponse<BucketDto>>> GetBucketsAsync(PopfileNetDbContext db, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        
        try
        {
            var totalCount = await db.Buckets.CountAsync();
            var buckets = await db.Buckets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BucketDto(b.Id, b.Name, b.Description ?? ""))
                .ToListAsync();
            
            return TypedResults.Ok(PagedApiResponse<BucketDto>.Success(buckets, page, pageSize, totalCount));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(PagedApiResponse<BucketDto>.Failure("BUCKETS_ERROR", ex.Message));
        }
    }

    private static async Task<Created<ApiResponse<BucketDto>>> CreateBucketAsync(BucketDto bucket, PopfileNetDbContext db)
    {
        try
        {
            var newBucket = new Bucket
            {
                Id = bucket.Id == Guid.Empty ? Guid.NewGuid() : bucket.Id,
                Name = bucket.Name,
                Description = bucket.Description
            };
            
            db.Buckets.Add(newBucket);
            await db.SaveChangesAsync();
            
            var result = new BucketDto(newBucket.Id, newBucket.Name, newBucket.Description);
            return TypedResults.Created($"/settings/buckets/{newBucket.Id}", ApiResponse<BucketDto>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Created($"/settings/buckets/{bucket.Id}", ApiResponse<BucketDto>.Failure("CREATE_BUCKET_ERROR", ex.Message));
        }
    }

    private static async Task<IResult> UpdateBucketAsync(Guid id, BucketDto bucket, PopfileNetDbContext db)
    {
        try
        {
            var existing = await db.Buckets.FindAsync(id);
            if (existing == null)
                return TypedResults.NotFound();

            existing.Name = bucket.Name;
            existing.Description = bucket.Description;
            
            await db.SaveChangesAsync();
            
            return TypedResults.Ok(ApiResponse<BucketDto>.Success(new BucketDto(existing.Id, existing.Name, existing.Description)));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(ApiResponse<BucketDto>.Failure("UPDATE_BUCKET_ERROR", ex.Message));
        }
    }

    private static async Task<IResult> DeleteBucketAsync(Guid id, PopfileNetDbContext db)
    {
        try
        {
            var bucket = await db.Buckets.FindAsync(id);
            if (bucket == null)
                return TypedResults.NotFound();

            db.Buckets.Remove(bucket);
            await db.SaveChangesAsync();
            
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(ApiResponse<bool>.Failure("DELETE_BUCKET_ERROR", ex.Message));
        }
    }
}
