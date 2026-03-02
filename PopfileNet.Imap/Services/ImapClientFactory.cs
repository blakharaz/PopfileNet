using MailKit.Net.Imap;
using PopfileNet.Imap;
using PopfileNet.Imap.Adapters;

namespace PopfileNet.Imap.Services;

public class ImapClientFactory : IImapClientFactory
{
    public IImapClient Create()
    {
        return new ImapClientAdapter(new ImapClient());
    }
}
