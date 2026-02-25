using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopfileNet.Classifier;
using PopfileNet.Common;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Cli;

public static class TestClassifierCommand
{
    public static Command CreateCommand(ImapSettings imapSettings, IDictionary<string, string> folderCategories, ILoggerFactory factory)
    {
        var testClassifierCommand = new Command("mail-classifier", "classifier test");

        var numClassificationOption = new Option<int>("num")
        {
            Description = "# mails to classify (skipped for training)", 
            DefaultValueFactory = _ => 5
        };
        testClassifierCommand.Options.Add(numClassificationOption);
        
        testClassifierCommand.SetAction(parseResult =>
        {
            var numClassification = parseResult.GetValue(numClassificationOption);
            
            return RunAsync(numClassification, imapSettings, folderCategories, factory);
        });

        return testClassifierCommand;
    }

    private static async Task<int> RunAsync(int numClassification, ImapSettings imapSettings, IDictionary<string, string> folderCategories, ILoggerFactory factory)
    {
        var cancellationTokenSource = new CancellationTokenSource();
       
        var imapService = new ImapClientService(
            Options.Create(imapSettings), factory.CreateLogger<ImapClientService>());

        var testData = new Dictionary<string, IList<Email>>();
        var dataset = new EmailClassificationDataSet();
        
        var folders = (await imapService.GetAllPersonalFoldersAsync(cancellationTokenSource.Token)).Skip(4).Take(3);
        
        foreach (var folder in folders)
        {
            var folderName = folder.FullName;
            
            var folderCategory = folderCategories.FirstOrDefault(entry => entry.Value.Equals(folderName, StringComparison.OrdinalIgnoreCase)).Key;
            
            if (folderCategory is null)
            {
                Console.WriteLine($"{folderName} is not assigned to a category");
                continue;
            }

            var ids = await imapService.FetchEmailIdsAsync(folderName, cancellationTokenSource.Token);
            Console.WriteLine("Found {0} mails in {1}", ids.Count, folderName);

            var trainingIds = ids.SkipLast(numClassification);
            Console.WriteLine("Getting mails in {0}", folderName);
            var trainingMails = await imapService.FetchEmailsAsync(trainingIds, folderName, cancellationTokenSource.Token);
            foreach (var mail in trainingMails)
            {
                dataset.AddMail(mail, folderCategory);
            }
                
            var testIds = ids.TakeLast(numClassification);
            testData[folderCategory] = await imapService.FetchEmailsAsync(testIds, folderName, cancellationTokenSource.Token);
            Console.WriteLine("Done.");
        }

        Console.WriteLine("Training classifier");
        var classifier = new NaiveBayesianClassifier();
        classifier.Train(dataset);
        Console.WriteLine("Done.");

        foreach (var testDataFolderEntry in testData)
        {
            foreach (var testMail in testDataFolderEntry.Value)
            {
                var predictedFolder = classifier.Predict(testMail);
                
                if (predictedFolder.PredictedLabel != folderCategories[testDataFolderEntry.Key])
                {
                    Console.WriteLine($"Misclassified mail from {testDataFolderEntry.Key} as {predictedFolder.PredictedLabel}");
                }
            }
        }

        return 0;
    }
}