namespace PopfileNet.Ui.Services;

public record FolderDto(
    string Id,
    string Name,
    string Status,
    DateTimeOffset? LastSyncAt,
    int LastSyncEmailCount,
    string? LastSyncError);
