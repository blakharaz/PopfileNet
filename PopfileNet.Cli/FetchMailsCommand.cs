using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Cli;

public static class FetchMailsCommand
{
    public static Command CreateCommand(ImapSettings settings, ILoggerFactory loggerFactory)
    {
        // Create limit option
        var limitOption = new Option<int>("--limit",
            "-l", "Number of emails to fetch") { DefaultValueFactory = _ => 40 };
        
        var fetchMailsCommand = new Command("fetch-mails", "Fetch emails from the IMAP server");
        fetchMailsCommand.Options.Add(limitOption);

        fetchMailsCommand.SetAction(async parseResult =>
        {
            var limit = parseResult.GetValue(limitOption);

            return await Run(settings, limit, loggerFactory);
        });

        return fetchMailsCommand;
    }

    private static async Task<int> Run(ImapSettings settings, int limit, ILoggerFactory loggerFactory)
    {
        var imapService = new ImapClientService(Options.Create(settings), new Logger<ImapClientService>(loggerFactory));
        await imapService.TestConnectionAsync();

        var folders = await imapService.GetAllPersonalFoldersAsync();
        if (folders.Count == 0)
        {
            Console.WriteLine("No folders found.");
            return 0;
        }

        var folder = folders[Math.Min(folders.Count - 1, 10)];
        var mailIds = await imapService.FetchEmailIdsAsync(folder.FullName);
        var mails = await imapService.FetchEmailsAsync(mailIds.Take(limit).ToList());

        Console.WriteLine($"Fetched {mails.Count} emails.");
        return 0;
    }
}