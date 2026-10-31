using PopfileNet.Common;
using PopfileNet.Database.Repositories;

namespace PopfileNet.Backend.Services;

/// <summary>
/// Walks all IMAP folders of the configured account and syncs new emails into the database,
/// recording a per-folder sync status (idle, syncing, error).
/// </summary>
public class EmailSyncService(IServiceScopeFactory scopeFactory, ILogger<EmailSyncService> logger)
{
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    /// <summary>
    /// Gets a value indicating whether a sync walk is currently executing.
    /// </summary>
    public bool IsBusy => _syncLock.CurrentCount == 0;

    /// <summary>
    /// Attempts to perform a complete sync walk over all IMAP folders.
    /// Returns <see langword="false"/> when another sync is already running; the walk is skipped in that case.
    /// Never throws for non-cancellation failures; per-folder failures are recorded on the folder itself.
    /// </summary>
    public async Task<bool> TryRunSyncAsync(CancellationToken ct = default)
    {
        if (!await _syncLock.WaitAsync(0, ct))
        {
            return false;
        }

        try
        {
            try
            {
                await SyncAllFoldersAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during email sync walk");
            }

            return true;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task SyncAllFoldersAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var imapService = scope.ServiceProvider.GetRequiredService<IImapService>();

        if (!await imapService.IsConfiguredAsync(ct))
        {
            logger.LogInformation("IMAP settings not configured; skipping sync");
            return;
        }

        var folders = await imapService.GetAllPersonalFoldersAsync(ct);

        var existingFolders = await emailRepository.GetAllFolderIdByNameAsync(ct);
        var newFolderNames = folders
            .Select(f => f.FullName)
            .Except(existingFolders.Keys)
            .ToList();

        if (newFolderNames.Count > 0)
        {
            var newFolders = newFolderNames.Select(name => new MailFolder { Name = name });
            await emailRepository.InsertFoldersAsync(newFolders, ct);
            existingFolders = await emailRepository.GetAllFolderIdByNameAsync(ct);
        }

        var existingImapUids = await emailRepository.GetExistingImapUidsByFolderAsync(ct);

        var totalSynced = 0;
        foreach (var folder in folders)
        {
            ct.ThrowIfCancellationRequested();

            var folderId = existingFolders[folder.FullName];
            totalSynced += await SyncFolderAsync(emailRepository, imapService, folder, folderId, existingImapUids, ct);
        }

        logger.LogInformation("Folder sync walk completed; {Count} new emails added", totalSynced);
    }

    private async Task<int> SyncFolderAsync(
        IEmailRepository emailRepository,
        IImapService imapService,
        FolderInfo folder,
        string folderId,
        Dictionary<string, string> existingImapUids,
        CancellationToken ct)
    {
        await emailRepository.MarkFolderSyncingAsync(folderId, ct);

        try
        {
            var ids = await imapService.FetchEmailIdsAsync(folder.FullName, ct);
            var newIds = ids
                .Where(id => !existingImapUids.ContainsKey($"{id.Validity}:{id.Id}"))
                .ToList();

            var syncedCount = 0;
            if (newIds.Count > 0)
            {
                var emails = await imapService.FetchEmailsAsync(newIds, folder.FullName, ct);
                foreach (var email in emails)
                {
                    email.Folder = folderId;
                }
                syncedCount = await emailRepository.InsertEmailsIgnoringDuplicatesAsync(emails, ct);
            }

            await emailRepository.CompleteFolderSyncAsync(folderId, syncedCount, ct);
            return syncedCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync folder {Folder}", folder.FullName);
            await emailRepository.FailFolderSyncAsync(folderId, ex.Message, CancellationToken.None);
            return 0;
        }
    }
}
