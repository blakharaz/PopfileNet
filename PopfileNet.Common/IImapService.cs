namespace PopfileNet.Common;

public interface IImapService
{
    /// <summary>
    /// Returns <see langword="true"/> when the service has enough settings to attempt a connection.
    /// </summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<IList<EmailId>> FetchEmailIdsAsync(string? folderName = null, CancellationToken cancellationToken = default);
    Task<IList<Email>> FetchEmailsAsync(IEnumerable<EmailId> ids, string? folderName = null, CancellationToken cancellationToken = default);
    Task<List<FolderInfo>> GetAllPersonalFoldersAsync(CancellationToken cancellationToken = default);
}
