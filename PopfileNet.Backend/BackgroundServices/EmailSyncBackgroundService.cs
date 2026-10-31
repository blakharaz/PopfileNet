using PopfileNet.Backend.Services;
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

        await ResetStaleFolderStatesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<EmailSyncService>();

                var started = await syncService.TryRunSyncAsync(stoppingToken);
                if (!started)
                {
                    logger.LogDebug("Manual sync already running; skipping scheduled sync");
                }
            }
            catch (OperationCanceledException)
            {
                // Service is shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during email sync");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }

    private async Task ResetStaleFolderStatesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

            var resetCount = await emailRepository.ResetStaleSyncingFoldersAsync(ct);
            if (resetCount > 0)
            {
                logger.LogInformation("Reset {Count} folder(s) stuck in syncing state", resetCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset stale folder sync states");
        }
    }
}
