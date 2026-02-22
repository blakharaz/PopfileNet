using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

var configuration = configBuilder.Build();

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());

var settings = configuration.GetSection("ImapSettings").Get<ImapSettings>()
    ?? throw new InvalidOperationException("ImapSettings configuration not found");

// Create root command
var rootCommand = new RootCommand("PopfileNet CLI - IMAP mail test utility");

// Create limit option
var limitOption = new Option<int>("--limit",
    "-l", "Number of emails to fetch") { DefaultValueFactory = _ => 40 };

// Create fetch-mails command
var fetchMailsCommand = new Command("fetch-mails", "Fetch emails from the IMAP server");
fetchMailsCommand.Options.Add(limitOption);

fetchMailsCommand.SetAction(async (pr) =>
{
    var limit = pr.GetValue(limitOption);
    
    var imapService = new ImapClientService(Options.Create(settings), new Logger<ImapClientService>(factory));
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
});

// Create test command
var testCommand = new Command("test", "Test IMAP connection and operations");
testCommand.Subcommands.Add(fetchMailsCommand);

rootCommand.Subcommands.Add(testCommand);

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();