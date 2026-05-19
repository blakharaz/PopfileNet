using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Classifier;
using PopfileNet.Common;
using PopfileNet.Database;
using System.Globalization;

namespace PopfileNet.Backend.Services;

/// <summary>
/// Runs classifier evaluation with train/test split and optional multi-run sampling.
/// </summary>
public class ClassifierEvaluationService(PopfileNetDbContext db)
{
    public async Task<EvaluationResult> RunEvaluationAsync(EvaluationRequest request, CancellationToken ct = default)
    {
        var allEmails = await FetchEmailsForFilter(request.FolderFilter, ct);

        if (allEmails.Count == 0)
        {
            throw new InvalidOperationException("No emails available for evaluation");
        }

        // Apply cutoff to separate training and test sets
        var (training, test) = CutoffAndSplit(allEmails, request.CutoffType, request.CutoffValue, request.TrainTestSplit);

        if (training.Count == 0)
        {
            throw new InvalidOperationException("Training set is empty after cutoff. Adjust the cutoff settings.");
        }

        if (test.Count == 0)
        {
            throw new InvalidOperationException("Test set is empty after cutoff. Adjust the cutoff settings.");
        }

        var runs = new List<RunResultDto>();
        for (int runNum = 0; runNum < request.NumberOfRuns; runNum++)
        {
            var actualTraining = training;
            var actualTest = test;

            if (request.CutoffType == "amount" && request.NumberOfRuns > 1)
            {
                // For amount cutoff with multiple runs, randomize each run
                var shuffledTrain = Shuffle(training);
                var shuffleCount = Math.Max(10, (int)(training.Count * (1 - request.TrainTestSplit)));
                actualTraining = [.. shuffledTrain.Take(shuffleCount)];
                actualTest = test.Except(actualTraining).ToList();
            }

            if (actualTest.Count == 0)
            {
                throw new InvalidOperationException("Not enough emails for the requested number of runs");
            }

            var runResult = await RunSingleAsync(actualTraining, actualTest, runNum + 1, request.BucketFilter);
            runs.Add(runResult);
        }

        if (request.NumberOfRuns == 1)
        {
            return new EvaluationResult(1, [runs[0]], null);
        }

        var aggregated = AggregateResults(runs);
        return new EvaluationResult(request.NumberOfRuns, runs, aggregated);
    }

    private async Task<List<Email>> FetchEmailsForFilter(string folderFilter, CancellationToken ct)
    {
        var baseQuery = db.Emails.AsQueryable();
        
        if (folderFilter != "all")
        {
            baseQuery = baseQuery.Where(e => e.Folder == folderFilter);
        }

        return await baseQuery.ToListAsync(ct);
    }

    private static (List<Email> training, List<Email> test) CutoffAndSplit(
        List<Email> emails, string cutoffType, string? cutoffValue, float trainTestRatio)
    {
        if (cutoffValue == null || emails.Count == 0)
        {
            return SplitByRatio(emails, trainTestRatio);
        }

        if (cutoffType == "amount" && int.TryParse(cutoffValue, out var count))
        {
            return HandleAmountCutoff(emails, count, trainTestRatio);
        }

        if (cutoffType == "date" && DateTime.TryParse(cutoffValue, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out var cutoffDate))
        {
            return HandleDateCutoff(emails, cutoffDate, trainTestRatio);
        }

        return SplitByRatio(emails, trainTestRatio);
    }

    private static (List<Email> training, List<Email> test) HandleAmountCutoff(
        List<Email> emails, int count, float ratio)
    {
        var sorted = emails.OrderByDescending(e => e.ReceivedDate).ToList();
        var trainingSet = sorted.Take(count).ToList();

        if (trainingSet.Count >= count)
        {
            return SplitByRatio(trainingSet, ratio);
        }

        // Not enough for the cutoff; use all emails with ratio split
        return SplitByRatio(emails, ratio);
    }

    private static (List<Email> training, List<Email> test) HandleDateCutoff(
        List<Email> emails, DateTime cutoffDate, float trainTestRatio)
    {
        // Use only emails before the cutoff date for training
        var beforeCutoff = emails.Where(e => e.ReceivedDate < cutoffDate).ToList();
        
        return SplitByRatio(beforeCutoff, trainTestRatio);
    }

    private static (List<Email> training, List<Email> test) SplitByRatio(List<Email> emails, float ratio)
    {
        var shuffled = Shuffle(emails);
        var splitIndex = Math.Max(1, (int)(shuffled.Count * ratio));
        return (shuffled.Take(splitIndex).ToList(), shuffled.Skip(splitIndex).ToList());
    }

    private static List<T> Shuffle<T>(List<T> list)
    {
        var result = new T[list.Count];
        list.CopyTo(result);

        for (int i = 0; i < result.Length - 1; i++)
        {
            var j = Random.Shared.Next(i, result.Length);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return [.. result];
    }

    private static async Task<RunResultDto> RunSingleAsync(
        List<Email> training, List<Email> test, int runNumber, string bucketFilter)
    {
        var classifier = new NaiveBayesianClassifier();
        var dataSet = new EmailClassificationDataSet();

        foreach (var email in training.Where(e => e.FolderNavigation?.Bucket != null))
        {
            dataSet.AddMail(email, email.FolderNavigation!.Bucket!.Name);
        }

        if (!dataSet.Data.Any())
        {
            throw new InvalidOperationException("No labeled data available for the selected filters");
        }

        classifier.Train(dataSet);

        var metrics = ComputeMetrics(test, bucketFilter, classifier);
        
        return new RunResultDto(
            runNumber, training.Count, test.Count,
            metrics.OverallAccuracy, metrics.Correct, metrics.Total,
            [.. metrics.BucketMetrics],
            [.. metrics.Mismatches]);
    }

    private static (float OverallAccuracy, int Correct, int Total, List<BucketMetricDto> BucketMetrics, List<MismatchDetailDto> Mismatches) ComputeMetrics(
        List<Email> test, string bucketFilter, NaiveBayesianClassifier classifier)
    {
        var predictions = CollectPredictions(test, bucketFilter, classifier);

        if (predictions.Count == 0)
        {
            return (0f, 0, test.Count, [], []);
        }

        var correct = predictions.Values.Count(v => v.actual == v.predicted);
        var total = predictions.Count;
        var overallAccuracy = correct / (float)total;
        var mismatches = CollectMismatches(predictions, test);
        var bucketMetrics = AggregateBucketMetrics(predictions).Values.Select(MetricToDto).ToList();

        return (overallAccuracy, correct, total, bucketMetrics, mismatches);
    }

    private static Dictionary<string, (string actual, string predicted)> CollectPredictions(
        List<Email> test, string bucketFilter, NaiveBayesianClassifier classifier)
    {
        var predictions = new Dictionary<string, (string actual, string predicted)>();

        foreach (var email in test.Where(e => e.FolderNavigation?.Bucket != null))
        {
            if (!MatchesFilter(email, bucketFilter))
                continue;

            try
            {
                var prediction = classifier.Predict(email);
                predictions[email.Id] = (email.FolderNavigation!.Bucket!.Name, prediction.PredictedLabel);
            }
            catch
            {
                // Skip emails that fail to predict
            }
        }

        return predictions;
    }

    private static List<MismatchDetailDto> CollectMismatches(
        Dictionary<string, (string actual, string predicted)> predictions, List<Email> test)
    {
        return predictions
            .Where(kv => kv.Value.actual != kv.Value.predicted)
            .Select(kv => new MismatchDetailDto(
                kv.Key,
                test.FirstOrDefault(e => e.Id == kv.Key)?.Subject ?? "",
                kv.Value.actual,
                kv.Value.predicted))
            .ToList();
    }

    private static Dictionary<string, BucketMetricAccumulator> AggregateBucketMetrics(
        Dictionary<string, (string actual, string predicted)> predictions)
    {
        var bucketMap = new Dictionary<string, BucketMetricAccumulator>();

        foreach (var pred in predictions.Values)
        {
            if (!bucketMap.TryGetValue(pred.actual, out var accumulator))
            {
                accumulator = new BucketMetricAccumulator(pred.actual);
                bucketMap[pred.actual] = accumulator;
            }

            if (pred.actual == pred.predicted)
            {
                accumulator.TruePositives++;
            }
            else
            {
                accumulator.FalseNegatives++;
            }
        }

        foreach (var pred in predictions.Values)
        {
            if (!bucketMap.TryGetValue(pred.predicted, out var accumulator))
            {
                accumulator = new BucketMetricAccumulator(pred.predicted);
                bucketMap[pred.predicted] = accumulator;
            }

            if (pred.actual != pred.predicted)
            {
                accumulator.FalsePositives++;
            }
        }

        return bucketMap;
    }

    private static bool MatchesFilter(Email email, string bucketFilter)
    {
        if (bucketFilter == "all")
        {
            return true;
        }

        var nav = email.FolderNavigation?.Bucket;
        return nav != null && (nav.Id == bucketFilter || nav.Name == bucketFilter);
    }

    private static BucketMetricDto MetricToDto(BucketMetricAccumulator acc)
    {
        return new BucketMetricDto(acc.BucketName, acc.TruePositives, acc.FalsePositives, 
            acc.FalseNegatives, acc.Precision, acc.Recall);
    }

    private static AggregatedMetricsDto AggregateResults(List<RunResultDto> runs)
    {
        var accuracies = runs.Select(r => r.Accuracy).ToList();

        var bucketAccumulators = new Dictionary<string, List<BucketMetricAccumulator>>();
        foreach (var run in runs)
        {
            foreach (var metric in run.BucketMetrics)
            {
                if (!bucketAccumulators.TryGetValue(metric.BucketName, out var accumulators))
                {
                    accumulators = new();
                    bucketAccumulators[metric.BucketName] = accumulators;
                }

                var acc = new BucketMetricAccumulator(metric.BucketName);
                acc.TruePositives = metric.TruePositives;
                acc.FalsePositives = metric.FalsePositives;
                acc.FalseNegatives = metric.FalseNegatives;
                accumulators.Add(acc);
            }
        }

        var perBucket = bucketAccumulators.Select(kv =>
        {
            var metrics = kv.Value;
            var meanPrecision = metrics.Average(m => m.Precision);
            var meanRecall = metrics.Average(m => m.Recall);
            return (kv.Key, new AggregatedBucketMetricDto(meanPrecision, meanRecall));
        }).ToDictionary(x => x.Key, x => x.Item2);

        return new AggregatedMetricsDto(
            accuracies.Average(),
            accuracies.Min(),
            accuracies.Max(),
            perBucket);
    }

    private sealed class BucketMetricAccumulator(string bucketName)
    {
        public string BucketName { get; } = bucketName;
        public int TruePositives { get; set; }
        public int FalsePositives { get; set; }
        public int FalseNegatives { get; set; }

        public float Precision => 
            (TruePositives + FalsePositives) > 0 ? TruePositives / (float)(TruePositives + FalsePositives) : 0f;
        
        public float Recall => 
            (TruePositives + FalseNegatives) > 0 ? TruePositives / (float)(TruePositives + FalseNegatives) : 0f;
    }
}
