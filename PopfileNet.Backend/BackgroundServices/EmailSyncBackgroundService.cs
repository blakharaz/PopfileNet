using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.BackgroundServices;

public class EmailSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    Common.IImapService imapService,
    ILogger<EmailSyncBackgroundService> logger,
    IConfiguration configuration)
    : BackgroundService
{
    private readonly TimeSpan _syncInterval = configuration.GetValue<TimeSpan>("SyncInterval", TimeSpan.FromMinutes(5));

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

        var folders = await imapService.GetAllPersonalFoldersAsync(cancellationToken);
        var allEmails = new List<Email>();

        foreach (var folder in folders)
        {
            var ids = await imapService.FetchEmailIdsAsync(folder.Name, cancellationToken);
            var existingIdStrings = await db.Emails.Select(e => e.Id).ToListAsync(cancellationToken);
            var newIds = ids.Where(id => !existingIdStrings.Contains($"{id.Validity}:{id.Id}")).ToList();

            if (newIds.Count > 0)
            {
                var emails = await imapService.FetchEmailsAsync(newIds, folder.Name, cancellationToken);
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
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Synced {Count} new emails", allEmails.Count);
        }
        else
        {
            logger.LogDebug("No new emails to sync");
        }
    }
}
