using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopfileNet.Cli;
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

// Create fetch-mails command
var fetchMailsCommand = FetchMailsCommand.CreateCommand(settings, factory);

// Create test command
var testCommand = new Command("test", "Test IMAP connection and operations");
testCommand.Subcommands.Add(fetchMailsCommand);

rootCommand.Subcommands.Add(testCommand);

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();