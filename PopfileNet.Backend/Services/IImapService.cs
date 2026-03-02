using PopfileNet.Common;

namespace PopfileNet.Backend.Services;

public interface IImapService
{
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<IList<EmailId>> FetchEmailIdsAsync(string? folderName = null, CancellationToken cancellationToken = default);
    Task<IList<Email>> FetchEmailsAsync(IEnumerable<EmailId> ids, string? folderName = null, CancellationToken cancellationToken = default);
    Task<List<FolderInfo>> GetAllPersonalFoldersAsync(CancellationToken cancellationToken = default);
}
