
using Microsoft.AspNetCore.Http.HttpResults;

namespace PopfileNet.Backend.Groups;

public static class JobsGroupExtensions
{
    public static WebApplication AddJobsGroup(this WebApplication app)
    {
        var group = app.MapGroup("/jobs");
        group.MapPost("/update-folder-list", UpdateFolderList);
        return app;
    }

    private static IResult UpdateFolderList() 
    {
        return Results.Ok();
    }
}
