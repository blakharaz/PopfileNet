using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

public static class MailsGroupExtensions
{
    public static WebApplication AddMailsGroup(this WebApplication app)
    {
        var group = app.MapGroup("/mails");
        
        group.MapGet("/folders", GetFoldersAsync);
        group.MapGet("/stats", GetMailStatsAsync);
        group.MapPost("/sync/{folderName}", SyncFolderAsync);
        group.MapGet("/{folderName}", GetMailsInFolderAsync);
        
        return app;
    }

    private static async Task<Ok<List<FolderInfo>>> GetFoldersAsync(IImapService imapService)
    {
        var folders = await imapService.GetAllPersonalFoldersAsync();
        return TypedResults.Ok(folders);
    }

    private static async Task<Ok<MailStats>> GetMailStatsAsync(PopfileNetDbContext db)
    {
        var totalEmails = await db.Emails.CountAsync();
        var emailsByFolder = await db.Emails
            .GroupBy(e => e.Folder)
            .Select(g => new { Folder = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Folder.ToString(), x => x.Count);
        
        return TypedResults.Ok(new MailStats(totalEmails, emailsByFolder));
    }

    private static async Task<Ok<SyncResult>> SyncFolderAsync(string folderName, PopfileNetDbContext db, IImapService imapService)
    {
        var ids = await imapService.FetchEmailIdsAsync(folderName);
        var existingIds = await db.Emails.Select(e => e.Id).ToListAsync();
        
        var newIds = ids.Where(id => !existingIds.Contains($"{id.Validity}:{id.Id}")).ToList();
        var emails = await imapService.FetchEmailsAsync(newIds, folderName);
        
        foreach (var email in emails)
        {
            email.Folder = Guid.Empty;
            db.Emails.Add(email);
        }
        
        await db.SaveChangesAsync();
        
        return TypedResults.Ok(new SyncResult(true, $"Synced {emails.Count} emails", emails.Count));
    }

    private static async Task<Ok<List<EmailDto>>> GetMailsInFolderAsync(string folderName, PopfileNetDbContext db)
    {
        var emails = await db.Emails.Take(100).ToListAsync();
        var dtos = emails.Select(e => new EmailDto(e.Id, e.Subject, e.FromAddress, e.ReceivedDate, "")).ToList();
        return TypedResults.Ok(dtos);
    }
}

public record MailStats(int TotalEmails, Dictionary<string, int> EmailsByFolder);

public record SyncResult(bool Success, string Message, int SyncedCount);

public record EmailDto(string Id, string Subject, string FromAddress, DateTime ReceivedDate, string BucketName);
