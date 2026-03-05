using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.Database.Repositories;

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
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
        var imapService = scope.ServiceProvider.GetRequiredService<IImapService>();

        if (!await imapService.IsConfiguredAsync(cancellationToken))
        {
            logger.LogInformation("IMAP settings not configured; skipping sync");
            return;
        }

        var folders = await imapService.GetAllPersonalFoldersAsync(cancellationToken);
        
        var existingFolders = await emailRepository.GetAllFolderIdByNameAsync(cancellationToken);
        var newFolderNames = folders.Select(f => f.FullName).Except(existingFolders.Keys).ToList();

        if (newFolderNames.Count > 0)
        {
            var newFolders = newFolderNames.Select(name => new MailFolder { Name = name });
            await emailRepository.InsertFoldersAsync(newFolders, cancellationToken);
            existingFolders = await emailRepository.GetAllFolderIdByNameAsync(cancellationToken);
        }

            var existingImapUids = await emailRepository.GetExistingImapUidsByFolderAsync(cancellationToken);

            List<Email> allEmails = [];

            foreach (var folder in folders)
            {
                var folderId = existingFolders[folder.FullName];
                var ids = await imapService.FetchEmailIdsAsync(folder.FullName, cancellationToken);
                var newIds = ids.Where(id => !existingImapUids.ContainsKey(id.Id)).ToList();

                if (newIds.Count > 0)
                {
                    var emails = await imapService.FetchEmailsAsync(newIds, folder.FullName, cancellationToken);
                    foreach (var email in emails)
                    {
                        email.Folder = folderId;
                    }
                    allEmails.AddRange(emails);
                }
            }

        if (allEmails.Count > 0)
        {
            var insertedCount = await emailRepository.InsertEmailsIgnoringDuplicatesAsync(allEmails, cancellationToken);
            logger.LogInformation("Synced {Count} new emails", insertedCount);
        }
        else
        {
            logger.LogDebug("No new emails to sync");
        }
    }
}
