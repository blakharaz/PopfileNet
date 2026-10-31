using PopfileNet.Common;

namespace PopfileNet.Database.Repositories;

public interface IEmailRepository
{
    Task<int> InsertEmailsIgnoringDuplicatesAsync(IEnumerable<Email> emails, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetExistingImapUidsByFolderAsync(CancellationToken ct = default);
    Task<Dictionary<string, string>> GetAllFolderIdByNameAsync(CancellationToken ct = default);
    Task InsertFoldersAsync(IEnumerable<MailFolder> folders, CancellationToken ct = default);

    /// <summary>
    /// Marks a folder as currently being synced.
    /// </summary>
    Task MarkFolderSyncingAsync(string folderId, CancellationToken ct = default);

    /// <summary>
    /// Records a successful folder sync.
    /// </summary>
    Task CompleteFolderSyncAsync(string folderId, int syncedEmailCount, CancellationToken ct = default);

    /// <summary>
    /// Records a failed folder sync with the given error message.
    /// </summary>
    Task FailFolderSyncAsync(string folderId, string error, CancellationToken ct = default);

    /// <summary>
    /// Resets folders stuck in the syncing state (e.g. after an application crash) to the error state.
    /// </summary>
    Task<int> ResetStaleSyncingFoldersAsync(CancellationToken ct = default);
}
