using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PopfileNet.Common;
using IMailFolder = MailKit.IMailFolder;

namespace PopfileNet.Imap.Services;

public interface IImapClientService
{
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<IList<EmailId>> FetchEmailIdsAsync(string? folderName = null, CancellationToken cancellationToken = default);
    Task<IList<Email>> FetchEmailsAsync(IEnumerable<EmailId> ids, string? folderName = null, CancellationToken cancellationToken = default);
    Task<IList<IMailFolder>> GetAllPersonalFoldersAsync(CancellationToken cancellationToken = default);
}
