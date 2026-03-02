namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents the result of a sync job.
/// </summary>
public record SyncJobResult(bool Success, string Message, int SyncedCount);
