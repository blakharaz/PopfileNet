using Microsoft.EntityFrameworkCore;
using PopfileNet.Database;
using PopfileNet.Common;

namespace PopfileNet.Backend.Services;

/// <summary>
/// EF Core-backed implementation of <see cref="IClassifierDataProvider"/>.
/// </summary>
public sealed class ClassifierDataProvider(PopfileNetDbContext db) : IClassifierDataProvider
{
    public async Task<List<Email>> FetchFilteredAsync(EmailFilterRequest request, CancellationToken ct = default)
    {
        var baseQuery = db.Emails.AsQueryable();

        if (request.FolderFilter != "all")
        {
            baseQuery = baseQuery.Where(e => e.Folder == request.FolderFilter);
        }

        return await baseQuery.ToListAsync(ct);
    }
}
