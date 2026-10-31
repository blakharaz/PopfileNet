using Microsoft.AspNetCore.Http.HttpResults;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;

namespace PopfileNet.Backend.Groups;

/// <summary>
/// Provides API endpoints for background jobs.
/// </summary>
public static class JobsGroupExtensions
{
    /// <summary>
    /// Maps the jobs endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication AddJobsGroup(this WebApplication app)
    {
        var group = app.MapGroup("/jobs").RequireAuthorization();
        group.MapPost("/sync", SyncAsync);
        group.MapPost("/update-folder-list", UpdateFolderListAsync);
        return app;
    }

    private static async Task<IResult> SyncAsync(EmailSyncService syncService)
    {
        if (syncService.IsBusy)
        {
            return TypedResults.Ok(ApiResponse<SyncJobResult>.Success(
                new SyncJobResult(true, "Sync is already running", 0)));
        }

        _ = Task.Run(() => syncService.TryRunSyncAsync());

        return TypedResults.Accepted(
            "/folders",
            ApiResponse<SyncJobResult>.Success(new SyncJobResult(true, "Sync started", 0)));
    }

    private static Ok<ApiResponse<bool>> UpdateFolderListAsync()
    {
        return TypedResults.Ok(ApiResponse<bool>.Success(true));
    }
}
