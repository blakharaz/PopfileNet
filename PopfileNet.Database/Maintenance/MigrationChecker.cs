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
            // Check for any non-system tables
            var hasAnyTables = await database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name NOT LIKE '__%'", ct) > 0;
            
            return hasAnyTables;
        }
        
        return false;
    }

    public async Task<bool> HasPendingMigrationsAsync(CancellationToken ct = default)
    {
        try
        {
            var pending = await database.GetPendingMigrationsAsync(ct);
            return pending.Any();
        }
        catch (Exception ex)
        {
            // If we can't check for pending migrations, assume there are none
            Console.Error.WriteLine($"Could not check for pending migrations: {ex.Message}");
            return false;
        }
    }

    public async Task ApplyMigrationsAsync(CancellationToken ct = default)
    {
        // Only apply migrations if there are no tables yet
        var hasAnyTables = await database.ExecuteSqlRawAsync(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name NOT LIKE '__%'", ct) > 0;
        
        if (!hasAnyTables)
        {
            await database.MigrateAsync(ct);
        }
    }
}