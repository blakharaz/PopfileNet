using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.Database.Repositories;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Cli;

public static class SyncMailsCommand
{
    public static Command CreateCommand(ImapSettings settings, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        var syncCommand = new Command("sync-mails", "Sync emails from IMAP server to database");

        var removeDeletedOption = new Option<bool>("--remove-deleted",
            "-r")
        {
            Description = "Remove emails from database that no longer exist on IMAP server",
            DefaultValueFactory = _ => false
        };
        syncCommand.Options.Add(removeDeletedOption);

        syncCommand.SetAction(async parseResult =>
        {
            var removeDeleted = parseResult.GetValue(removeDeletedOption);
            return await Run(settings, loggerFactory, serviceProvider, removeDeleted);
        });

        return syncCommand;
    }

    private static async Task<int> Run(ImapSettings settings, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, bool removeDeleted)
    {
        var logger = loggerFactory.CreateLogger("SyncMails");
        var imapClientFactory = new ImapClientFactory();
        var imapService = new ImapClientService(Options.Create(settings), loggerFactory.CreateLogger<ImapClientService>(), imapClientFactory);

        logger.LogInformation("Testing IMAP connection...");
        await imapService.TestConnectionAsync();

        logger.LogInformation("Fetching folders...");
        var imapFolders = await imapService.GetAllPersonalFoldersAsync();
        if (imapFolders.Count == 0)
        {
            Console.WriteLine("No folders found.");
            return 0;
        }

        Console.WriteLine($"Found {imapFolders.Count} folders.");

        using var scope = serviceProvider.CreateScope();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();

        var existingFolders = await emailRepository.GetAllFolderIdByNameAsync();
        var newFolderNames = imapFolders.Select(f => f.FullName ?? f.Name).Except(existingFolders.Keys).ToList();

        if (newFolderNames.Count > 0)
        {
            var newFolders = newFolderNames.Select(name => new MailFolder { Name = name });
            await emailRepository.InsertFoldersAsync(newFolders);
            existingFolders = await emailRepository.GetAllFolderIdByNameAsync();
        }

        var existingImapUids = await emailRepository.GetExistingImapUidsByFolderAsync();

        int totalEmailsSynced = 0;
        var allImapEmailIds = new HashSet<string>();

        foreach (var imapFolder in imapFolders)
        {
            var fullName = imapFolder.FullName ?? imapFolder.Name;
            var folderId = existingFolders[fullName];
            Console.WriteLine($"\nSyncing folder: {fullName}");

            var mailIds = await imapService.FetchEmailIdsAsync(fullName);
            Console.WriteLine($"  Found {mailIds.Count} emails");

            foreach (var id in mailIds)
            {
                allImapEmailIds.Add(id.Id.ToString());
            }

            var newIds = mailIds.Where(id => !existingImapUids.ContainsKey(id.Id.ToString())).ToList();

            int folderSynced = 0;
            int batchSize = 50;

            for (int i = 0; i < newIds.Count; i += batchSize)
            {
                var batchIds = newIds.Skip(i).Take(batchSize).ToList();
                var emails = await imapService.FetchEmailsAsync(batchIds, fullName);

                foreach (var email in emails)
                {
                    email.Folder = folderId;
                }

                var insertedCount = await emailRepository.InsertEmailsIgnoringDuplicatesAsync(emails);
                folderSynced += insertedCount;

                Console.WriteLine($"  Processed {Math.Min(i + batchSize, newIds.Count)}/{newIds.Count} - Synced: {folderSynced}");
            }
            
            totalEmailsSynced += folderSynced;
        }

        if (removeDeleted)
        {
            Console.WriteLine("\nChecking for deleted emails...");
            Console.WriteLine("Remove deleted not implemented via repository - skipping.");
        }

        Console.WriteLine($"\n=== Sync Complete ===");
        Console.WriteLine($"Total emails synced: {totalEmailsSynced}");

        return 0;
    }
}
