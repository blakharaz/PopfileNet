using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Classifier;
using PopfileNet.Common;
using System.Globalization;

namespace PopfileNet.Backend.Services;

/// <summary>
/// Provides email data for classifier evaluation.
/// </summary>
public interface IClassifierDataProvider
{
    Task<List<Email>> FetchFilteredAsync(EmailFilterRequest request, CancellationToken ct = default);
}

/// <summary>
/// Runs classifier evaluation with train/test split and optional multi-run sampling.
/// </summary>
public class ClassifierEvaluationService(IClassifierDataProvider dataProvider)
{
    private readonly Random _random = new();


    private async Task<List<Email>> FetchEmailsForFilter(string folderFilter, CancellationToken ct)
        => await dataProvider.FetchFilteredAsync(new(folderFilter), ct);

    public async Task<EvaluationResult> RunEvaluationAsync(EvaluationRequest request, CancellationToken ct = default)
    {
        var allEmails = await FetchEmailsForFilter(request.FolderFilter, ct);

        if (!allEmails.Any())
            throw new InvalidOperationException("No emails available for evaluation");

        // Apply cutoff to separate training and test sets
        var (training, test) = CutoffAndSplit(allEmails, request.CutoffType, request.CutoffValue, request.TrainTestSplit);

        if (!training.Any())
            throw new InvalidOperationException("Training set is empty after cutoff. Adjust the cutoff settings.");

        if (!test.Any())
            throw new InvalidOperationException("Test set is empty after cutoff. Adjust the cutoff settings.");

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

            if (!actualTest.Any())
                throw new InvalidOperationException("Not enough emails for the requested number of runs");

            var runResult = await RunSingleAsync(actualTraining, actualTest, runNum + 1, request.BucketFilter, ct);
            runs.Add(runResult);
        }

        EvaluationResult result;

        if (request.NumberOfRuns == 1)
        {
            result = new EvaluationResult(
                1,
                [runs[0]],
                null);
        }
        else
        {
            var aggregated = AggregateResults(runs);
            result = new EvaluationResult(request.NumberOfRuns, runs, aggregated);
        }

        return result;
    }

    private (List<Email> training, List<Email> test) CutoffAndSplit(
        List<Email> emails, string cutoffType, string? cutoffValue, float trainTestRatio)
    {
        if (cutoffValue == null || !emails.Any())
            return SplitByRatio(emails, trainTestRatio);

        if (cutoffType == "amount" && int.TryParse(cutoffValue, out var count))
        {
            // Sort by received date descending, take `count` most recent as training set
            var sorted = emails.OrderByDescending(e => e.ReceivedDate).ToList();
            var trainingSet = sorted.Take(count).ToList();

            if (trainingSet.Count >= count)
                return SplitByRatio(trainingSet, trainTestRatio);

            // Not enough for the cutoff; use all emails with ratio split
            return SplitByRatio(emails, trainTestRatio);
        }

        if (cutoffType == "date" && DateTime.TryParse(cutoffValue, out var cutoffDate))
        {
            // Use only emails before the cutoff date for training
            var beforeCutoff = emails.Where(e => e.ReceivedDate < cutoffDate).ToList();

            return SplitByRatio(beforeCutoff, trainTestRatio);
        }

        return SplitByRatio(emails, trainTestRatio);
    }

    private (List<Email> training, List<Email> test) SplitByRatio(List<Email> emails, float ratio)
    {
        var shuffled = Shuffle(emails);
        var splitIndex = Math.Max(1, (int)(shuffled.Count * ratio));
        return (shuffled.Take(splitIndex).ToList(), shuffled.Skip(splitIndex).ToList());
    }

    private List<T> Shuffle<T>(List<T> list)
    {
        var result = new T[list.Count];
        list.CopyTo(result);

        for (int i = 0; i < result.Length - 1; i++)
        {
            var j = _random.Next(i, result.Length);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return [.. result];
    }

    private async Task<RunResultDto> RunSingleAsync(
        List<Email> training, List<Email> test, int runNumber, string bucketFilter, CancellationToken ct)
    {
        var classifier = new NaiveBayesianClassifier();
        var dataSet = new EmailClassificationDataSet();

        foreach (var email in training.Where(e => e.FolderNavigation?.Bucket != null))
            dataSet.AddMail(email, email.FolderNavigation!.Bucket!.Name);

        if (!dataSet.Data.Any())
            throw new InvalidOperationException("No labeled data available for the selected filters");

        classifier.Train(dataSet);

        var metrics = ComputeMetrics(test, bucketFilter, classifier, ct);

        return new RunResultDto(
            runNumber, training.Count, test.Count,
            metrics.OverallAccuracy, metrics.Correct, metrics.Total,
            [.. metrics.BucketMetrics],
            [.. metrics.Mismatches]);
    }

    private (float OverallAccuracy, int Correct, int Total, List<BucketMetricDto> BucketMetrics, List<MismatchDetailDto> Mismatches) ComputeMetrics(
        List<Email> test, string bucketFilter, NaiveBayesianClassifier classifier, CancellationToken ct)
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

        if (!predictions.Any())
            return (0f, 0, test.Count, [], []);

        var correct = predictions.Values.Count(v => v.actual == v.predicted);
        var total = predictions.Count;
        var overallAccuracy = total > 0 ? correct / (float)total : 0f;

        // Per-bucket metrics: count TP/FP/FN for each bucket
        var bucketMap = new Dictionary<string, BucketMetricAccumulator>();
        foreach (var pred in predictions.Values)
        {
            if (!bucketMap.ContainsKey(pred.actual))
                bucketMap[pred.actual] = new BucketMetricAccumulator(pred.actual);

            if (pred.actual == pred.predicted)
                bucketMap[pred.actual].TruePositives++;
            else
                bucketMap[pred.actual].FalseNegatives++;
        }

        foreach (var pred in predictions.Values)
        {
            if (!bucketMap.ContainsKey(pred.predicted))
                bucketMap[pred.predicted] = new BucketMetricAccumulator(pred.predicted);

            if (pred.actual != pred.predicted)
                bucketMap[pred.predicted].FalsePositives++;
        }

        var mismatches = predictions
            .Where(kv => kv.Value.actual != kv.Value.predicted)
            .Select(kv => new MismatchDetailDto(
                kv.Key,
                test.FirstOrDefault(e => e.Id == kv.Key)?.Subject ?? "",
                kv.Value.actual,
                kv.Value.predicted))
            .ToList();

        return (overallAccuracy, correct, total, bucketMap.Values.Select(MetricToDto).ToList(), mismatches);
    }

    private static bool MatchesFilter(Email email, string bucketFilter)
    {
        if (bucketFilter == "all") return true;
        var nav = email.FolderNavigation?.Bucket;
        return nav != null && (nav.Id == bucketFilter || nav.Name == bucketFilter);
    }

    private static BucketMetricDto MetricToDto(BucketMetricAccumulator acc)
    {
        return new BucketMetricDto(acc.BucketName, acc.TruePositives, acc.FalsePositives, 
            acc.FalseNegatives, acc.Precision, acc.Recall);
    }

    private AggregatedMetricsDto AggregateResults(List<RunResultDto> runs)
    {
        var accuracies = runs.Select(r => r.Accuracy).ToList();

        var bucketAccumulators = new Dictionary<string, List<BucketMetricAccumulator>>();
        foreach (var run in runs)
        {
            foreach (var metric in run.BucketMetrics)
            {
                if (!bucketAccumulators.ContainsKey(metric.BucketName))
                    bucketAccumulators[metric.BucketName] = new();

                var acc = new BucketMetricAccumulator(metric.BucketName);
                acc.TruePositives = metric.TruePositives;
                acc.FalsePositives = metric.FalsePositives;
                acc.FalseNegatives = metric.FalseNegatives;
                bucketAccumulators[metric.BucketName].Add(acc);
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

    private class BucketMetricAccumulator(string bucketName)
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
