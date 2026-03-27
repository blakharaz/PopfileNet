namespace PopfileNet.Backend.Services;

public interface ISettingsService
{
    Task<Models.AppSettings> GetSettingsAsync(CancellationToken ct = default);
    Task<Models.ImapSettingsDto> GetImapSettingsOnlyAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(Models.AppSettings settings, CancellationToken ct = default);
    Task<IReadOnlyList<Models.FolderMappingDto>> GetFolderMappingsAsync(CancellationToken ct = default);
    Task SetFolderMappingAsync(string folderName, string? bucketId, CancellationToken ct = default);
    Task RemoveFolderMappingAsync(string folderName, CancellationToken ct = default);
}
