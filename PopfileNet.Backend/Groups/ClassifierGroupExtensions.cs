using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Classifier;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

/// <summary>
/// Provides API endpoints for the email classifier.
/// </summary>
public static class ClassifierGroupExtensions
{
    private const string DefaultOwnerId = "default";

    /// <summary>
    /// Maps the classifier endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication AddClassifierGroup(this WebApplication app)
    {
        var group = app.MapGroup("/classifier").RequireAuthorization();
        
        group.MapGet("/status", GetStatusAsync);
        group.MapGet("/dev-mode", () => TypedResults.Ok(ApiResponse<bool>.Success(app.Configuration.GetValue<bool>("DevMode"))));
        group.MapPost("/train", TrainAsync);
        group.MapPost("/predict", PredictAsync);
        
        return app;
    }

    /// <summary>
    /// Resolves the model owner id for the current authenticated user.
    /// </summary>
    private static string ResolveOwnerId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(id) ? DefaultOwnerId : id;
    }

    internal static async Task<Ok<ApiResponse<ClassifierStatus>>> GetStatusAsync(
        ClassifierManager manager, ClaimsPrincipal user)
    {
        var meta = await manager.GetMetaAsync(ResolveOwnerId(user));
        var isTrained = meta != null;
        return TypedResults.Ok(ApiResponse<ClassifierStatus>.Success(
            new ClassifierStatus(isTrained, meta?.TrainingSampleCount ?? 0)));
    }

    internal static async Task<IResult> TrainAsync(
        PopfileNetDbContext db, ClassifierManager manager, ClaimsPrincipal user)
    {
        var emails = await db.Emails
            .Include(e => e.FolderNavigation)
            .ThenInclude(f => f!.Bucket)
            .ToListAsync();

        var validEmails = emails.Where(e => e.FolderNavigation?.Bucket != null).ToList();

        if (!validEmails.Any())
            return TypedResults.BadRequest(ApiResponse<bool>.Failure("NO_TRAINING_DATA", "No training data available"));

        var trainingData = new EmailClassificationDataSet();
        foreach (var email in validEmails)
        {
            var bucketName = email.FolderNavigation!.Bucket!.Name;
            trainingData.AddMail(email, bucketName);
        }

        var classifier = new NaiveBayesianClassifier();
        classifier.Train(trainingData);
        await manager.SaveModelAsync(ResolveOwnerId(user), classifier);

        return TypedResults.Ok(ApiResponse<bool>.Success(true));
    }

    internal static async Task<IResult> PredictAsync(
        PredictRequest request, PopfileNetDbContext db, ClassifierManager manager, ClaimsPrincipal user)
    {
        var classifier = await manager.GetModelAsync(ResolveOwnerId(user));
        if (classifier == null)
            return TypedResults.Ok(ApiResponse<PredictionResult>.Success(new PredictionResult("", 0, [])));

        var email = await db.Emails.FindAsync(request.EmailId);
        if (email == null)
            return TypedResults.NotFound(ApiResponse<PredictionResult>.Failure("EMAIL_NOT_FOUND", "Email not found"));

        var prediction = classifier.Predict(email);
        
        var result = new PredictionResult(
            prediction.PredictedLabel, 
            prediction.Scores.Length > 0 ? prediction.Scores[0] : 0,
            new Dictionary<string, float> { { prediction.PredictedLabel, prediction.Scores.Length > 0 ? prediction.Scores[0] : 0 } }
        );
        return TypedResults.Ok(ApiResponse<PredictionResult>.Success(result));
    }
}