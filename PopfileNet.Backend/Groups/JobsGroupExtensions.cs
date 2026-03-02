using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

/// <summary>
/// Provides API endpoints for background jobs.
/// </summary>
public static class JobsGroupExtensions
{
    /// <summary>
    /// Maps the jobs endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication AddJobsGroup(this WebApplication app)
    {
        var group = app.MapGroup("/jobs");
        group.MapPost("/sync", SyncAsync);
        group.MapPost("/update-folder-list", UpdateFolderListAsync);
        return app;
    }

    private static async Task<IResult> SyncAsync(PopfileNetDbContext db, IImapService imapService)
    {
        if (!await imapService.IsConfiguredAsync())
        {
            return TypedResults.InternalServerError(
                ApiResponse<SyncJobResult>.Failure("IMAP_NOT_CONFIGURED", "IMAP not configured"));
        }

        var folders = await imapService.GetAllPersonalFoldersAsync();
        var allEmails = new List<Email>();
        
        foreach (var folder in folders)
        {
            var ids = await imapService.FetchEmailIdsAsync(folder.Name);
            var existingIdStrings = await db.Emails.Select(e => e.Id).ToListAsync();
            var newIds = ids.Where(id => !existingIdStrings.Contains($"{id.Validity}:{id.Id}")).ToList();
            
            if (newIds.Count > 0)
            {
                var emails = await imapService.FetchEmailsAsync(newIds, folder.Name);
                foreach (var email in emails)
                {
                    email.Folder = Guid.Empty;
                }
                allEmails.AddRange(emails);
            }
        }
        
        if (allEmails.Count > 0)
        {
            db.Emails.AddRange(allEmails);
            await db.SaveChangesAsync();
        }
        
        var result = new SyncJobResult(true, $"Synced {allEmails.Count} emails", allEmails.Count);
        return TypedResults.Ok(ApiResponse<SyncJobResult>.Success(result));
    }

    private static Ok<ApiResponse<bool>> UpdateFolderListAsync()
    {
        return TypedResults.Ok(ApiResponse<bool>.Success(true));
    }
}
