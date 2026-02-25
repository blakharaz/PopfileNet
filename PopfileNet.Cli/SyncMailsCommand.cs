using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;
using IMailFolder = MailKit.IMailFolder;

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
        var imapService = new ImapClientService(Options.Create(settings), new Logger<ImapClientService>(loggerFactory));

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
        var dbContext = scope.ServiceProvider.GetRequiredService<PopfileNetDbContext>();

        var folderIdMap = new Dictionary<string, Guid>();
        foreach (var imapFolder in imapFolders)
        {
            var fullName = imapFolder.FullName ?? imapFolder.Name;
            var dbFolder = await dbContext.MailFolders.FirstOrDefaultAsync(f => f.Name == fullName);
            if (dbFolder == null)
            {
                dbFolder = new MailFolder { Name = fullName };
                dbContext.MailFolders.Add(dbFolder);
                await dbContext.SaveChangesAsync();
            }
            folderIdMap[fullName] = dbFolder.Id;
        }

        int totalEmailsSynced = 0;
        int totalEmailsSkipped = 0;
        var allImapEmailIds = new HashSet<string>();

        foreach (var imapFolder in imapFolders)
        {
            var fullName = imapFolder.FullName ?? imapFolder.Name;
            var folderId = folderIdMap[fullName];
            Console.WriteLine($"\nSyncing folder: {fullName}");

            var mailIds = await imapService.FetchEmailIdsAsync(fullName);
            Console.WriteLine($"  Found {mailIds.Count} emails");

            foreach (var id in mailIds)
            {
                allImapEmailIds.Add(id.ToString());
            }

            int folderSynced = 0;
            int folderSkipped = 0;
            int batchSize = 50;

            for (int i = 0; i < mailIds.Count; i += batchSize)
            {
                var batchIds = mailIds.Skip(i).Take(batchSize).ToList();
                var emails = await imapService.FetchEmailsAsync(batchIds, fullName);

                foreach (var email in emails)
                {
                    var existingEmail = await dbContext.Emails.FindAsync(email.Id);
                    if (existingEmail != null)
                    {
                        if (existingEmail.Folder != folderId)
                        {
                            existingEmail.Folder = folderId;
                            folderSynced++;
                            totalEmailsSynced++;
                        }
                        else
                        {
                            folderSkipped++;
                            totalEmailsSkipped++;
                        }
                        continue;
                    }

                    var dbEmail = new Email
                    {
                        Id = email.Id,
                        Subject = email.Subject,
                        FromAddress = email.FromAddress,
                        ToAddresses = email.ToAddresses,
                        Body = email.Body,
                        ReceivedDate = email.ReceivedDate,
                        IsHtml = email.IsHtml,
                        Folder = folderId
                    };

                    foreach (var header in email.Headers)
                    {
                        dbEmail.Headers.Add(new MailHeader
                        {
                            EmailId = email.Id,
                            Name = header.Name,
                            Value = header.Value
                        });
                    }

                    dbContext.Emails.Add(dbEmail);
                    folderSynced++;
                    totalEmailsSynced++;
                }

                await dbContext.SaveChangesAsync();
                Console.WriteLine($"  Processed {Math.Min(i + batchSize, mailIds.Count)}/{mailIds.Count} - Synced: {folderSynced}, Skipped: {folderSkipped}");
            }
        }

        if (removeDeleted)
        {
            Console.WriteLine("\nChecking for deleted emails...");
            var dbEmailIds = await dbContext.Emails.Select(e => e.Id).ToListAsync();
            var idsToDelete = dbEmailIds.Except(allImapEmailIds).ToList();

            if (idsToDelete.Count > 0)
            {
                await dbContext.Emails.Where(e => idsToDelete.Contains(e.Id)).ExecuteDeleteAsync();
            }
            Console.WriteLine($"Removed {idsToDelete.Count} deleted emails from database.");
        }

        Console.WriteLine($"\n=== Sync Complete ===");
        Console.WriteLine($"Total emails synced/updated: {totalEmailsSynced}");
        Console.WriteLine($"Total emails skipped (already exists): {totalEmailsSkipped}");

        return 0;
    }
}
