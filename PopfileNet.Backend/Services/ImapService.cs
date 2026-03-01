using PopfileNet.Common;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Backend.Services;

public interface IImapService
{
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<IList<EmailId>> FetchEmailIdsAsync(string? folderName = null, CancellationToken cancellationToken = default);
    Task<IList<Email>> FetchEmailsAsync(IEnumerable<EmailId> ids, string? folderName = null, CancellationToken cancellationToken = default);
    Task<List<FolderInfo>> GetAllPersonalFoldersAsync(CancellationToken cancellationToken = default);
}

public class FolderInfo(string name, int count)
{
    public string Name { get; } = name;
    public int Count { get; } = count;
}

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
