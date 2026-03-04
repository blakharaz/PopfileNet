using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopfileNet.Common;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Backend.Services;

public class ImapService(ISettingsService settingsService, ILogger<ImapClientService> logger) : Common.IImapService
{
    private readonly ISettingsService _settingsService = settingsService;
    private readonly ILogger<ImapClientService> _logger = logger;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetImapSettingsAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(settings.Server) || string.IsNullOrEmpty(settings.Username))
        {
            return false;
        }
        
        var imapClientService = new ImapClientService(Options.Create(settings), _logger);
        return await imapClientService.TestConnectionAsync(cancellationToken);
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
        var imapSettings = await _settingsService.GetImapSettingsOnlyAsync(ct);
        return new ImapSettings
        {
            Server = imapSettings.Server,
            Port = imapSettings.Port,
            Username = imapSettings.Username,
            Password = imapSettings.Password,
            UseSsl = imapSettings.UseSsl,
            MaxParallelConnections = imapSettings.MaxParallelConnections
        };
    }
}
