namespace PopfileNet.Database.Maintenance;

public interface IMigrationChecker
{
    Task<bool> HasLegacyTablesAsync(CancellationToken ct = default);
    Task<bool> HasPendingMigrationsAsync(CancellationToken ct = default);
    Task ApplyMigrationsAsync(CancellationToken ct = default);
}
