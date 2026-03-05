using MailKit;
using MailKit.Net.Imap;
using IMailFolder = MailKit.IMailFolder;

namespace PopfileNet.Imap.Adapters;

public class ImapClientAdapter(ImapClient client) : IImapClient
{
    public bool IsConnected => client.IsConnected;
    public IMailFolder Inbox => client.Inbox;
    public FolderNamespace[] PersonalNamespaces => client.PersonalNamespaces.ToArray();

    public Task ConnectAsync(string host, int port, bool useSsl, CancellationToken cancellationToken = default)
        => client.ConnectAsync(host, port, useSsl, cancellationToken);

    public Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        => client.AuthenticateAsync(username, password, cancellationToken);

    public Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default)
        => client.DisconnectAsync(quit, cancellationToken);

    public Task<IMailFolder> GetFolderAsync(string name, CancellationToken cancellationToken = default)
        => client.GetFolderAsync(name, cancellationToken);

    public Task<IList<IMailFolder>> GetFoldersAsync(FolderNamespace namespaceValue, CancellationToken cancellationToken = default)
        => client.GetFoldersAsync(namespaceValue, cancellationToken: cancellationToken);

    public void Dispose()
    {
        client.Dispose();
    }
}
