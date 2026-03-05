using PopfileNet.Common;

namespace PopfileNet.Database.Repositories;

public interface IEmailRepository
{
    Task<int> InsertEmailsIgnoringDuplicatesAsync(IEnumerable<Email> emails, CancellationToken ct = default);
    Task<Dictionary<string, string>> GetExistingImapUidsByFolderAsync(CancellationToken ct = default);
    Task<Dictionary<string, string>> GetAllFolderIdByNameAsync(CancellationToken ct = default);
    Task InsertFoldersAsync(IEnumerable<MailFolder> folders, CancellationToken ct = default);
}
