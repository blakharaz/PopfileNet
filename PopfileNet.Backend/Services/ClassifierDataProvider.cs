using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Services;

/// <summary>
/// EF Core-backed implementation of <see cref="IClassifierDataProvider"/>.
/// </summary>
public sealed class ClassifierDataProvider(PopfileNetDbContext db) : IClassifierDataProvider
{
    public async Task<List<Email>> FetchFilteredAsync(EmailFilterRequest request, CancellationToken ct = default)
    {
        IQueryable<Email> baseQuery = db.Emails
            .Include(e => e.FolderNavigation)
                .ThenInclude(f => f!.Bucket);

        if (request.FolderFilter != "all")
        {
            baseQuery = baseQuery.Where(e => e.Folder == request.FolderFilter);
        }

        return await baseQuery.AsNoTracking().ToListAsync(ct);
    }
}
