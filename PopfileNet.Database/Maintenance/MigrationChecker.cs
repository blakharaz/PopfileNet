namespace PopfileNet.Database.Maintenance;

public class MigrationChecker(IDatabaseFacade database) : IMigrationChecker
{
    public async Task<bool> HasLegacyTablesAsync(CancellationToken ct = default)
    {
        if (!await database.CanConnectAsync(ct))
            return false;
        
        var historyTableExists = await database.ExecuteSqlRawAsync(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory'", ct) > 0;
        
        if (!historyTableExists)
        {
            var hasAnyTables = await database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name NOT LIKE '__%'", ct) > 0;
            
            return hasAnyTables;
        }
        
        return false;
    }

    public async Task<bool> HasPendingMigrationsAsync(CancellationToken ct = default)
    {
        var pending = await database.GetPendingMigrationsAsync(ct);
        return pending.Any();
    }

    public async Task ApplyMigrationsAsync(CancellationToken ct = default)
    {
        await database.MigrateAsync(ct);
    }
}
