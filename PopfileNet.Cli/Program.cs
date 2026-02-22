// See https://aka.ms/new-console-template for more information

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

Console.WriteLine("Hello, World!");

var settings = configuration.GetSection("ImapSettings").Get<ImapSettings>()
    ?? throw new InvalidOperationException("ImapSettings configuration not found");

var imapService = new ImapClientService(Options.Create(settings), new Logger<ImapClientService>(factory));
await imapService.TestConnectionAsync();

var folders = await imapService.GetAllPersonalFoldersAsync();
var folder = folders[Math.Min(folders.Count, 10)];
var mailIds = await imapService.FetchEmailIdsAsync(folder.FullName);
var mails = await imapService.FetchEmailsAsync(mailIds.Take(40));

Console.WriteLine(mails.Count);