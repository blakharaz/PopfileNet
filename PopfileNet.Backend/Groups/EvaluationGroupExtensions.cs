using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

public static class EvaluationGroupExtensions
{
    public static WebApplication AddEvaluationGroup(this WebApplication app)
    {
        var devMode = app.Configuration.GetValue<bool>("DevMode");
        if (!devMode)
            return app; // Skip registration entirely when not in dev mode

        var group = app.MapGroup("/evaluation");

        group.MapPost("/run", RunEvaluationAsync);
        group.MapGet("/config", GetConfigAsync);

        return app;
    }

    internal static async Task<IResult> RunEvaluationAsync(
        EvaluationRequest request, ClassifierEvaluationService evaluationService)
    {
        try
        {
            var result = await evaluationService.RunEvaluationAsync(request);
            return TypedResults.Ok(ApiResponse<EvaluationResult>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ApiResponse<EvaluationResult>.Failure("INVALID_CONFIG", ex.Message));
        }
    }

    internal static async Task<IResult> GetConfigAsync(PopfileNetDbContext db)
    {
        var folders = await (db.MailFolders.Select(f => f.Name)).Distinct().ToListAsync<string>();
        var buckets = await (from b in db.Buckets select new { b.Id, b.Name }).ToListAsync();

        return TypedResults.Ok(ApiResponse<object>.Success(new
        {
            Folders = folders,
            Buckets = buckets
        }));
    }
}
