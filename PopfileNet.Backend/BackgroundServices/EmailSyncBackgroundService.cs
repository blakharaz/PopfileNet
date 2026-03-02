using Microsoft.EntityFrameworkCore;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.BackgroundServices;

public class EmailSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailSyncBackgroundService> logger,
    IConfiguration configuration)
    : BackgroundService
{
    private readonly TimeSpan _syncInterval = configuration.GetValue("SyncInterval", TimeSpan.FromMinutes(5));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email sync background service started with interval {Interval}", _syncInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during email sync");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }

    private async Task SyncEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PopfileNetDbContext>();
        var imapService = scope.ServiceProvider.GetRequiredService<IImapService>();

        if (!await imapService.IsConfiguredAsync(cancellationToken))
        {
            logger.LogInformation("IMAP settings not configured; skipping sync");
            return;
        }

        var folders = await imapService.GetAllPersonalFoldersAsync(cancellationToken);
        
        var existingFolders = await db.MailFolders.ToDictionaryAsync(f => f.Name, f => f.Id, cancellationToken);
        var newFolderNames = folders.Select(f => f.Name).Except(existingFolders.Keys);

        bool folderAdded = false;
        foreach (var folderName in newFolderNames)
        {
            var newFolder = new MailFolder { Name = folderName };
            db.MailFolders.Add(newFolder);
            folderAdded = true;
        }
        
        if (folderAdded)
        {
            await db.SaveChangesAsync(cancellationToken);
            existingFolders = await db.MailFolders.ToDictionaryAsync(f => f.Name, f => f.Id, cancellationToken);
        }

        HashSet<string> existingImapUids = [.. await db.Emails.Select(e => e.ImapUid).ToListAsync(cancellationToken)];

        List<Email> allEmails = [];

        foreach (var folder in folders)
        {
            var folderId = existingFolders[folder.Name];
            var ids = await imapService.FetchEmailIdsAsync(folder.Name, cancellationToken);
            var newIds = ids.Where(id => !existingImapUids.Contains($"{folder.Name}:{id.Validity}:{id.Id}")).ToList();

            if (newIds.Count > 0)
            {
                var emails = await imapService.FetchEmailsAsync(newIds, folder.Name, cancellationToken);
                foreach (var email in emails)
                {
                    email.Folder = folderId;
                }
                allEmails.AddRange(emails);
            }
        }

        if (allEmails.Count > 0)
        {
            db.Emails.AddRange(allEmails);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Synced {Count} new emails", allEmails.Count);
        }
        else
        {
            logger.LogDebug("No new emails to sync");
        }
    }
}
