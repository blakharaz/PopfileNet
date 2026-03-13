using Microsoft.EntityFrameworkCore;

namespace PopfileNet.Database.DatabaseMaintenance;

public class MigrationChecker(PopfileNetDbContext dbContext) : IMigrationChecker
{
    public async Task<bool> HasLegacyTablesAsync(CancellationToken ct = default)
    {
        if (!await dbContext.Database.CanConnectAsync(ct))
            return false;
        
        var historyTableExists = await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory'", ct) > 0;
        
        if (!historyTableExists)
        {
            var hasAnyTables = await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name NOT LIKE '__%'", ct) > 0;
            
            return hasAnyTables;
        }
        
        return false;
    }

    public async Task<bool> HasPendingMigrationsAsync(CancellationToken ct = default)
    {
        var pending = await dbContext.Database.GetPendingMigrationsAsync(ct);
        return pending.Any();
    }

    public async Task ApplyMigrationsAsync(CancellationToken ct = default)
    {
        await dbContext.Database.MigrateAsync(ct);
    }
}
