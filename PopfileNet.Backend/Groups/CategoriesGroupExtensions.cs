using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

/// <summary>
/// Provides API endpoints for classification categories.
/// </summary>
public static class CategoriesGroupExtensions
{
    /// <summary>
    /// Maps the categories endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication AddCategoriesGroup(this WebApplication app)
    {
        var group = app.MapGroup("/categories");
        group.MapGet("/", GetCategoriesAsync);
        return app;
    }

    private static async Task<Ok<PagedApiResponse<BucketDto>>> GetCategoriesAsync(PopfileNetDbContext db, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        
        var totalCount = await db.Buckets.CountAsync();
        var buckets = await db.Buckets
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BucketDto(b.Id, b.Name, b.Description ?? ""))
            .ToListAsync();
        
        return TypedResults.Ok(PagedApiResponse<BucketDto>.Success(buckets, page, pageSize, totalCount));
    }
}
