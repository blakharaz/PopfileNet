using MailKit;

namespace PopfileNet.Imap;

public interface IImapClient : IDisposable
{
    bool IsConnected { get; }
    IMailFolder Inbox { get; }
    FolderNamespace[] PersonalNamespaces { get; }

    Task ConnectAsync(string host, int port, bool useSsl, CancellationToken cancellationToken = default);
    Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default);
    Task<IMailFolder> GetFolderAsync(string name, CancellationToken cancellationToken = default);
    Task<IList<IMailFolder>> GetFoldersAsync(FolderNamespace namespaceValue, CancellationToken cancellationToken = default);
}
