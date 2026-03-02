using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Classifier;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

/// <summary>
/// Provides API endpoints for the email classifier.
/// </summary>
public static class ClassifierGroupExtensions
{
    private static NaiveBayesianClassifier? _classifier;
    private static bool _isTrained;

    /// <summary>
    /// Maps the classifier endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication AddClassifierGroup(this WebApplication app)
    {
        var group = app.MapGroup("/classifier");
        
        group.MapGet("/status", GetStatusAsync);
        group.MapPost("/train", TrainAsync);
        group.MapPost("/predict", PredictAsync);
        
        return app;
    }

    private static Ok<ApiResponse<ClassifierStatus>> GetStatusAsync()
    {
        return TypedResults.Ok(ApiResponse<ClassifierStatus>.Success(new ClassifierStatus(_isTrained, 0)));
    }

    private static async Task<IResult> TrainAsync(PopfileNetDbContext db)
    {
        try
        {
            var emails = await db.Emails.ToListAsync();
            
            if (!emails.Any())
                return TypedResults.Ok(ApiResponse<bool>.Failure("NO_TRAINING_DATA", "No training data available"));

            var trainingData = new EmailClassificationDataSet();
            foreach (var email in emails.Where(e => e.Folder != Guid.Empty))
            {
                trainingData.AddMail(email, email.Folder.ToString());
            }

            _classifier = new NaiveBayesianClassifier();
            _classifier.Train(trainingData);
            _isTrained = true;

            return TypedResults.Ok(ApiResponse<bool>.Success(true));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(ApiResponse<bool>.Failure("TRAIN_ERROR", ex.Message));
        }
    }

    private static async Task<Ok<ApiResponse<PredictionResult>>> PredictAsync(PredictRequest request, PopfileNetDbContext db)
    {
        try
        {
            if (_classifier == null || !_isTrained)
                return TypedResults.Ok(ApiResponse<PredictionResult>.Success(new PredictionResult("", 0, [])));

            var email = await db.Emails.FindAsync(request.EmailId);
            if (email == null)
                return TypedResults.Ok(ApiResponse<PredictionResult>.Failure("EMAIL_NOT_FOUND", "Email not found"));

            var prediction = _classifier.Predict(email);
            
            var result = new PredictionResult(
                prediction.PredictedLabel, 
                prediction.Scores.Length > 0 ? prediction.Scores[0] : 0,
                new Dictionary<string, float> { { prediction.PredictedLabel, prediction.Scores.Length > 0 ? prediction.Scores[0] : 0 } }
            );
            return TypedResults.Ok(ApiResponse<PredictionResult>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(ApiResponse<PredictionResult>.Failure("PREDICT_ERROR", ex.Message));
        }
    }
}
