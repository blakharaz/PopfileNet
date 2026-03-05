using Microsoft.AspNetCore.Http.HttpResults;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.Database.Repositories;

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

    private static async Task<IResult> SyncAsync(IEmailRepository emailRepository, IImapService imapService)
    {
        if (!await imapService.IsConfiguredAsync())
        {
            return TypedResults.InternalServerError(
                ApiResponse<SyncJobResult>.Failure("IMAP_NOT_CONFIGURED", "IMAP not configured"));
        }

        var folders = await imapService.GetAllPersonalFoldersAsync();
        
        var existingFolders = await emailRepository.GetAllFolderIdByNameAsync();
        var newFolderNames = folders.Select(f => f.FullName).Except(existingFolders.Keys).ToList();
        
        if (newFolderNames.Count > 0)
        {
            var newFolders = newFolderNames.Select(name => new MailFolder { Name = name });
            await emailRepository.InsertFoldersAsync(newFolders);
            existingFolders = await emailRepository.GetAllFolderIdByNameAsync();
        }

        var existingImapUids = await emailRepository.GetExistingImapUidsByFolderAsync();
        
        List<Email> allEmails = [];
        
        foreach (var folder in folders)
        {
            var folderId = existingFolders[folder.FullName];
            var ids = await imapService.FetchEmailIdsAsync(folder.FullName);
            var newIds = ids.Where(id => !existingImapUids.ContainsKey($"{folder.FullName}:{id.Validity}:{id.Id}")).ToList();
            
            if (newIds.Count > 0)
            {
                var emails = await imapService.FetchEmailsAsync(newIds, folder.FullName);
                foreach (var email in emails)
                {
                    email.Folder = folderId;
                }
                allEmails.AddRange(emails);
            }
        }
        
        if (allEmails.Count > 0)
        {
            var insertedCount = await emailRepository.InsertEmailsIgnoringDuplicatesAsync(allEmails);
            return TypedResults.Ok(ApiResponse<SyncJobResult>.Success(new SyncJobResult(true, $"Synced {insertedCount} emails", insertedCount)));
        }
        
        return TypedResults.Ok(ApiResponse<SyncJobResult>.Success(new SyncJobResult(true, $"Synced {allEmails.Count} emails", allEmails.Count)));
    }

    private static Ok<ApiResponse<bool>> UpdateFolderListAsync()
    {
        return TypedResults.Ok(ApiResponse<bool>.Success(true));
    }
}
