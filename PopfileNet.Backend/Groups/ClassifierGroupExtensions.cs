using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Classifier;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

public static class ClassifierGroupExtensions
{
    private static NaiveBayesianClassifier? _classifier;
    private static bool _isTrained;

    public static WebApplication AddClassifierGroup(this WebApplication app)
    {
        var group = app.MapGroup("/classifier");
        
        group.MapGet("/status", GetStatusAsync);
        group.MapPost("/train", TrainAsync);
        group.MapPost("/predict", PredictAsync);
        
        return app;
    }

    private static Ok<ClassifierStatus> GetStatusAsync()
    {
        return TypedResults.Ok(new ClassifierStatus(_isTrained, 0));
    }

    private static async Task<IResult> TrainAsync(PopfileNetDbContext db)
    {
        var emails = await db.Emails.ToListAsync();
        
        if (!emails.Any())
            return Results.BadRequest("No training data available");

        var trainingData = new EmailClassificationDataSet();
        foreach (var email in emails.Where(e => e.Folder != Guid.Empty))
        {
            trainingData.AddMail(email, email.Folder.ToString());
        }

        _classifier = new NaiveBayesianClassifier();
        _classifier.Train(trainingData);
        _isTrained = true;

        return Results.Ok(new { Success = true, Message = "Model trained successfully" });
    }

    private static async Task<Ok<PredictionResult>> PredictAsync(PredictRequest request, PopfileNetDbContext db)
    {
        if (_classifier == null || !_isTrained)
            return TypedResults.Ok(new PredictionResult("", 0, []));

        var email = await db.Emails.FindAsync(request.EmailId);
        if (email == null)
            return TypedResults.Ok(new PredictionResult("", 0, []));

        var prediction = _classifier.Predict(email);
        
        return TypedResults.Ok(new PredictionResult(
            prediction.PredictedLabel, 
            prediction.Scores.Length > 0 ? prediction.Scores[0] : 0,
            new Dictionary<string, float> { { prediction.PredictedLabel, prediction.Scores.Length > 0 ? prediction.Scores[0] : 0 } }
        ));
    }
}

public record ClassifierStatus(bool IsTrained, int TrainingDataCount);

public record PredictRequest(string EmailId);

public record PredictionResult(string PredictedBucket, float Confidence, Dictionary<string, float> AllProbabilities);
