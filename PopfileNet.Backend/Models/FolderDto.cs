namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents a mail folder.
/// </summary>
public record FolderDto(
    string Id,
    string Name,
    string Status,
    DateTimeOffset? LastSyncAt,
    int LastSyncEmailCount,
    string? LastSyncError);
