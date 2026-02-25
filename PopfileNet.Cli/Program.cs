using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopfileNet.Cli;
using PopfileNet.Database;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

var configuration = configBuilder.Build();

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());

var imapSettings = configuration.GetSection("ImapSettings").Get<ImapSettings>()
    ?? throw new InvalidOperationException("ImapSettings configuration not found");
var categoryMapping = configuration.GetSection("Classifications").Get<IDictionary<string, string>>()
    ?? throw new InvalidOperationException("Classifications configuration not found");
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not found");

var services = new ServiceCollection();
services.AddEmailAnalysisDatabase(connectionString);
services.AddSingleton(imapSettings);
services.AddSingleton(factory);

var serviceProvider = services.BuildServiceProvider();

// Create root command
var rootCommand = new RootCommand("PopfileNet CLI - IMAP mail test utility");


// Create test command
var testCommand = new Command("test", "Test IMAP connection and operations");
testCommand.Subcommands.Add(FetchMailsCommand.CreateCommand(imapSettings, factory));
testCommand.Subcommands.Add(TestClassifierCommand.CreateCommand(imapSettings, categoryMapping, factory));
testCommand.Subcommands.Add(SyncMailsCommand.CreateCommand(imapSettings, factory, serviceProvider));

rootCommand.Subcommands.Add(testCommand);

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();