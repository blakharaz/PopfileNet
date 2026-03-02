using MailKit;
using MailKit.Net.Imap;
using PopfileNet.Imap;

namespace PopfileNet.Imap.Adapters;

public class ImapClientAdapter(MailKit.Net.Imap.ImapClient client) : IImapClient
{
    private readonly MailKit.Net.Imap.ImapClient _client = client;

    public bool IsConnected => _client.IsConnected;
    public IMailFolder Inbox => _client.Inbox;
    public FolderNamespace[] PersonalNamespaces => _client.PersonalNamespaces.ToArray();

    public Task ConnectAsync(string host, int port, bool useSsl, CancellationToken cancellationToken = default)
        => _client.ConnectAsync(host, port, useSsl, cancellationToken);

    public Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        => _client.AuthenticateAsync(username, password, cancellationToken);

    public Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default)
        => _client.DisconnectAsync(quit, cancellationToken);

    public Task<IMailFolder> GetFolderAsync(string name, CancellationToken cancellationToken = default)
        => _client.GetFolderAsync(name, cancellationToken);

    public Task<IList<IMailFolder>> GetFoldersAsync(FolderNamespace namespaceValue, CancellationToken cancellationToken = default)
        => _client.GetFoldersAsync(namespaceValue, cancellationToken: cancellationToken);

    public void Dispose()
    {
        _client.Dispose();
    }
}
