using PopfileNet.Common;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Backend.Services;

public class ImapService(ImapSettings settings) : IImapService
{
    private readonly ImapSettings _settings = settings;

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!string.IsNullOrEmpty(_settings.Server) && !string.IsNullOrEmpty(_settings.Username));
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
}
