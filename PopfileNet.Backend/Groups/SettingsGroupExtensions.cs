using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

public static class SettingsGroupExtensions
{
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

    private static async Task<Ok<AppSettings>> GetSettingsAsync(PopfileNetDbContext db)
    {
        var buckets = await db.Buckets.ToListAsync();
        var folders = await db.MailFolders.ToListAsync();
        
        var settings = new AppSettings
        {
            ImapSettings = new ImapSettingsDto(),
            Buckets = buckets.Select(b => new BucketDto(b.Id, b.Name, b.Description)).ToList(),
            FolderMappings = folders.Select(f => new FolderMappingDto(f.Name, f.Bucket?.Id)).ToList()
        };
        
        return TypedResults.Ok(settings);
    }

    private static async Task<IResult> SaveSettingsAsync(AppSettings settings, PopfileNetDbContext db, IConfiguration config)
    {
        config["ImapSettings:Server"] = settings.ImapSettings?.Server;
        config["ImapSettings:Port"] = settings.ImapSettings?.Port.ToString();
        config["ImapSettings:Username"] = settings.ImapSettings?.Username;
        config["ImapSettings:Password"] = settings.ImapSettings?.Password;
        config["ImapSettings:UseSsl"] = settings.ImapSettings?.UseSsl.ToString();

        return Results.Ok();
    }

    private static async Task<Ok<bool>> TestConnectionAsync(IImapService imapClient)
    {
        var result = await imapClient.TestConnectionAsync();
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<BucketDto>>> GetBucketsAsync(PopfileNetDbContext db)
    {
        var buckets = await db.Buckets.ToListAsync();
        var dtos = buckets.Select(b => new BucketDto(b.Id, b.Name, b.Description)).ToList();
        return TypedResults.Ok(dtos);
    }

    private static async Task<Created<BucketDto>> CreateBucketAsync(BucketDto bucket, PopfileNetDbContext db)
    {
        var newBucket = new Bucket
        {
            Id = bucket.Id == Guid.Empty ? Guid.NewGuid() : bucket.Id,
            Name = bucket.Name,
            Description = bucket.Description
        };
        
        db.Buckets.Add(newBucket);
        await db.SaveChangesAsync();
        
        return TypedResults.Created($"/settings/buckets/{newBucket.Id}", new BucketDto(newBucket.Id, newBucket.Name, newBucket.Description));
    }

    private static async Task<IResult> UpdateBucketAsync(Guid id, BucketDto bucket, PopfileNetDbContext db)
    {
        var existing = await db.Buckets.FindAsync(id);
        if (existing == null)
            return Results.NotFound();

        existing.Name = bucket.Name;
        existing.Description = bucket.Description;
        
        await db.SaveChangesAsync();
        
        return Results.Ok(new BucketDto(existing.Id, existing.Name, existing.Description));
    }

    private static async Task<IResult> DeleteBucketAsync(Guid id, PopfileNetDbContext db)
    {
        var bucket = await db.Buckets.FindAsync(id);
        if (bucket == null)
            return Results.NotFound();

        db.Buckets.Remove(bucket);
        await db.SaveChangesAsync();
        
        return Results.NoContent();
    }
}

public class AppSettings
{
    public ImapSettingsDto? ImapSettings { get; set; }
    public List<BucketDto> Buckets { get; set; } = [];
    public List<FolderMappingDto> FolderMappings { get; set; } = [];
}

public record ImapSettingsDto(
    string Server = "",
    int Port = 993,
    string Username = "",
    string Password = "",
    bool UseSsl = true);

public record BucketDto(Guid Id, string Name, string Description);

public record FolderMappingDto(string FolderName, Guid? BucketId);
