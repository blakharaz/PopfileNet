namespace PopfileNet.Database.Migration;

public interface IDatabaseFacade
{
    Task<bool> CanConnectAsync(CancellationToken ct = default);
    Task<int> ExecuteSqlRawAsync(string sql, CancellationToken ct = default);
    Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken ct = default);
    Task MigrateAsync(CancellationToken ct = default);
}