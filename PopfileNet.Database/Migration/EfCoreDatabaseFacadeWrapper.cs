using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace PopfileNet.Database.Migration;

[ExcludeFromCodeCoverage(Justification = "Thin wrapper for testable database operations, not worth testing directly.")]
public class EfCoreDatabaseFacadeWrapper(PopfileNetDbContext dbContext) : IDatabaseFacade
{
    public Task<bool> CanConnectAsync(CancellationToken ct = default) => 
        dbContext.Database.CanConnectAsync(ct);
    
    public Task<int> ExecuteSqlRawAsync(string sql, CancellationToken ct = default) => 
        dbContext.Database.ExecuteSqlRawAsync(sql, ct);
    
    public Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken ct = default) => 
        dbContext.Database.GetPendingMigrationsAsync(ct);
    
    public Task MigrateAsync(CancellationToken ct = default) => 
        dbContext.Database.MigrateAsync(ct);
}
