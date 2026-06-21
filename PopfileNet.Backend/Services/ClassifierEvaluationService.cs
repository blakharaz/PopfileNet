using System.Globalization;
using PopfileNet.Backend.Models;
using PopfileNet.Classifier;
using PopfileNet.Common;

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


    /// <summary>
    /// Fetches emails filtered by the given folder filter.
    /// </summary>
    private async Task<List<Email>> FetchEmailsForFilter(string folderFilter, CancellationToken ct)
        => await dataProvider.FetchFilteredAsync(new(folderFilter), ct);

    public async Task<EvaluationResult> RunEvaluationAsync(EvaluationRequest request, CancellationToken ct = default)
    {
        var allEmails = await FetchEmailsForFilter(request.FolderFilter, ct);

        if (allEmails.Count == 0)
            throw new InvalidOperationException("No emails available for evaluation");

        // Apply cutoff to separate training and test sets
        var (training, test) = CutoffAndSplit(allEmails, request.CutoffType, request.CutoffValue, request.TrainTestSplit);

        if (training.Count == 0)
            throw new InvalidOperationException("Training set is empty after cutoff. Adjust the cutoff settings.");

        if (test.Count == 0)
            throw new InvalidOperationException("Test set is empty after cutoff. Adjust the cutoff settings.");

        var runs = new List<RunResultDto>();
        for (int runNum = 0; runNum < request.NumberOfRuns; runNum++)
        {
            var actualTraining = training;
            var actualTest = test;

            if (request is { CutoffType: "amount", NumberOfRuns: > 1 })
            {
                // For amount cutoff with multiple runs, randomize each run
                var shuffledTrain = Shuffle(training);
                var shuffleCount = Math.Max(10, (int)(training.Count * (1 - request.TrainTestSplit)));
                actualTraining = [.. shuffledTrain.Take(shuffleCount)];
                actualTest = test.Except(actualTraining).ToList();
            }

            if (actualTest.Count == 0)
                throw new InvalidOperationException("Not enough emails for the requested number of runs");

            var runResult = RunSingle(actualTraining, actualTest, runNum + 1, request.BucketFilter);
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

    /// <summary>
    /// Applies cutoffs and splits emails into training and test sets.
    /// </summary>
    private (List<Email> training, List<Email> test) CutoffAndSplit(
        List<Email> emails, string cutoffType, string? cutoffValue, float trainTestRatio)
    {
        if (cutoffValue == null || emails.Count == 0)
            return SplitByRatio(emails, trainTestRatio);

        var trainingSet = GetTrainingSet(emails, cutoffType, cutoffValue);

        if (trainingSet.Count == 0)
            throw new InvalidOperationException("No training emails available after applying the cutoff.");

        return SplitByRatio(trainingSet, trainTestRatio);
    }

    /// <summary>
    /// Filters emails by cutoff type and returns the training set.
    /// </summary>
    private List<Email> GetTrainingSet(List<Email> emails, string cutoffType, string? cutoffValue)
    {
        if (cutoffType == "amount" && int.TryParse(cutoffValue, out var count))
        {
            return GetMostRecentN(emails, count);
        }

        if (cutoffType == "date")
        {
            if (!DateTime.TryParseExact(
                    cutoffValue,
                    "yyyy-MM-dd",
                    DateTimeFormatInfo.InvariantInfo,
                    DateTimeStyles.None,
                    out var cutoffDate))
            {
                return emails; // Invalid date format - use all emails
            }

            return emails.Where(e => e.ReceivedDate < cutoffDate).ToList();
        }

        // No valid cutoff or invalid type - use all emails
        return emails;
    }

    /// <summary>
    /// Returns the N most recent emails sorted by received date.
    /// </summary>
    private static List<Email> GetMostRecentN(List<Email> emails, int count)
    {
        var sorted = emails.OrderByDescending(e => e.ReceivedDate).ToList();
        return sorted.Take(count).ToList();
    }

    /// <summary>
    /// Splits emails into training and test sets using a given ratio.
    /// </summary>
    private (List<Email> training, List<Email> test) SplitByRatio(List<Email> emails, float ratio)
    {
        var shuffled = Shuffle(emails);
        var splitIndex = Math.Max(1, (int)(shuffled.Count * ratio));
        return (shuffled.Take(splitIndex).ToList(), shuffled.Skip(splitIndex).ToList());
    }

    /// <summary>
    /// Shuffles a list of emails using Fisher-Yates algorithm.
    /// </summary>
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

    /// <summary>
    /// Runs a single classifier evaluation run synchronously.
    /// </summary>
    private static RunResultDto RunSingle(
        List<Email> training, List<Email> test, int runNumber, string bucketFilter)
    {
        var classifier = new NaiveBayesianClassifier();
        var dataSet = new EmailClassificationDataSet();

        foreach (var email in training.Where(e => e.FolderNavigation?.Bucket != null))
            dataSet.AddMail(email, email.FolderNavigation!.Bucket!.Name);

        if (!dataSet.Data.Any())
            throw new InvalidOperationException("No labeled data available for the selected filters");

        classifier.Train(dataSet);

        var predictions = ComputePredictions(test, bucketFilter, classifier);
        var metrics = ComputeMetrics(predictions, test);

        return new RunResultDto(
            runNumber, training.Count, test.Count,
            metrics.OverallAccuracy, metrics.Correct, metrics.Total,
            [.. metrics.BucketMetrics],
            [.. metrics.Mismatches]);
    }

    /// <summary>
    /// Predicts labels for test emails, skipping any that fail to classify.
    /// </summary>
    private static Dictionary<string, (string actual, string predicted)> ComputePredictions(
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

    /// <summary>
    /// Computes overall and per-bucket classification metrics.
    /// </summary>
    private static (float OverallAccuracy, int Correct, int Total, List<BucketMetricDto> BucketMetrics, List<MismatchDetailDto> Mismatches) ComputeMetrics(
        Dictionary<string, (string actual, string predicted)> predictions, List<Email> test)
    {
        if (predictions.Count == 0)
            return (0f, 0, test.Count, [], []);

        var correct = predictions.Values.Count(v => v.actual == v.predicted);
        var total = predictions.Count;
        var overallAccuracy = correct / (float)total;

        var bucketMetrics = ComputeBucketMetrics(predictions);
        var mismatches = ComputeMismatches(predictions, test);

        return (overallAccuracy, correct, total, bucketMetrics, mismatches);
    }

    /// <summary>
    /// Computes per-bucket precision and recall metrics.
    /// </summary>
    private static List<BucketMetricDto> ComputeBucketMetrics(
        Dictionary<string, (string actual, string predicted)> predictions)
    {
        var bucketMap = new Dictionary<string, BucketMetricAccumulator>();

        // Count TP and FN for each actual bucket
        foreach (var pred in predictions.Values)
        {
            if (!bucketMap.ContainsKey(pred.actual))
                bucketMap[pred.actual] = new BucketMetricAccumulator(pred.actual);

            if (pred.actual == pred.predicted)
                bucketMap[pred.actual].TruePositives++;
            else
                bucketMap[pred.actual].FalseNegatives++;
        }

        // Count FP for each predicted bucket
        foreach (var pred in predictions.Values)
        {
            if (!bucketMap.ContainsKey(pred.predicted))
                bucketMap[pred.predicted] = new BucketMetricAccumulator(pred.predicted);

            if (pred.actual != pred.predicted)
                bucketMap[pred.predicted].FalsePositives++;
        }

        return bucketMap.Values.Select(MetricToDto).ToList();
    }

    /// <summary>
    /// Identifies classification mismatches with email subjects.
    /// </summary>
    private static List<MismatchDetailDto> ComputeMismatches(
        Dictionary<string, (string actual, string predicted)> predictions, List<Email> test)
    {
        var mismatches = predictions
            .Where(kv => kv.Value.actual != kv.Value.predicted)
            .Select(kv => new MismatchDetailDto(
                kv.Key,
                test.FirstOrDefault(e => e.Id == kv.Key)?.Subject ?? "",
                kv.Value.actual,
                kv.Value.predicted))
            .ToList();

        return mismatches;
    }

    /// <summary>
    /// Checks if an email matches the bucket filter.
    /// </summary>
    private static bool MatchesFilter(Email email, string bucketFilter)
    {
        if (bucketFilter == "all") return true;
        var nav = email.FolderNavigation?.Bucket;
        return nav != null && (nav.Id == bucketFilter || nav.Name == bucketFilter);
    }

    /// <summary>
    /// Converts a bucket metric accumulator to DTO format.
    /// </summary>
    private static BucketMetricDto MetricToDto(BucketMetricAccumulator acc)
    {
        return new BucketMetricDto(acc.BucketName, acc.TruePositives, acc.FalsePositives, 
            acc.FalseNegatives, acc.Precision, acc.Recall);
    }

    /// <summary>
    /// Aggregates multiple run results into summary statistics.
    /// </summary>
    private static AggregatedMetricsDto AggregateResults(List<RunResultDto> runs)
    {
        var accuracies = runs.Select(r => r.Accuracy).ToList();

        var perBucketMetrics = ComputePerBucketMetrics(runs);

        return new AggregatedMetricsDto(
            accuracies.Average(),
            accuracies.Min(),
            accuracies.Max(),
            perBucketMetrics);
    }

    private static Dictionary<string, AggregatedBucketMetricDto> ComputePerBucketMetrics(List<RunResultDto> runs)
    {
        var bucketAccumulators = new Dictionary<string, List<BucketMetricAccumulator>>();

        foreach (var run in runs)
        {
            foreach (var metric in run.BucketMetrics)
            {
                if (!bucketAccumulators.ContainsKey(metric.BucketName))
                    bucketAccumulators[metric.BucketName] = [];

                var acc = new BucketMetricAccumulator(metric.BucketName)
                {
                    TruePositives = metric.TruePositives,
                    FalsePositives = metric.FalsePositives,
                    FalseNegatives = metric.FalseNegatives
                };
                bucketAccumulators[metric.BucketName].Add(acc);
            }
        }

        var perBucketMetrics = bucketAccumulators.Select(kv =>
        {
            var metricsList = kv.Value;
            var meanPrecision = metricsList.Average(m => m.Precision);
            var meanRecall = metricsList.Average(m => m.Recall);
            return (kv.Key, new AggregatedBucketMetricDto(meanPrecision, meanRecall));
        }).ToDictionary(x => x.Key, x => x.Item2);

        return perBucketMetrics;
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
