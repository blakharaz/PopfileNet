using PopfileNet.Common;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Backend.Services;

public class ImapService(ISettingsService settingsService) : Common.IImapService
{
    private readonly ISettingsService _settingsService = settingsService;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetImapSettingsAsync(cancellationToken);
        return !string.IsNullOrEmpty(settings.Server) && !string.IsNullOrEmpty(settings.Username);
    }

    public Task<IList<EmailId>> FetchEmailIdsAsync(string? folderName = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IList<EmailId>>([]);
    }

    public Task<IList<Email>> FetchEmailsAsync(IEnumerable<EmailId> ids, string? folderName = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IList<Email>>([]);
    }

    public Task<List<FolderInfo>> GetAllPersonalFoldersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<FolderInfo>());
    }

    private async Task<ImapSettings> GetImapSettingsAsync(CancellationToken ct = default)
    {
        var appSettings = await _settingsService.GetSettingsAsync(ct);
        return new ImapSettings
        {
            Server = appSettings.ImapSettings?.Server ?? "",
            Port = appSettings.ImapSettings?.Port ?? 993,
            Username = appSettings.ImapSettings?.Username ?? "",
            Password = appSettings.ImapSettings?.Password ?? "",
            UseSsl = appSettings.ImapSettings?.UseSsl ?? true,
            MaxParallelConnections = appSettings.ImapSettings?.MaxParallelConnections ?? 4
        };
    }
}
