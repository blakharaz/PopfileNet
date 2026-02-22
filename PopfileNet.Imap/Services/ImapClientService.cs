using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PopfileNet.Common;
using PopfileNet.Imap.Exceptions;
using PopfileNet.Imap.Models;
using PopfileNet.Imap.Settings;
using IMailFolder = MailKit.IMailFolder;

namespace PopfileNet.Imap.Services;

public class ImapClientService(
    IOptions<ImapSettings> settings,
    ILogger<ImapClientService> logger)
    : IImapClientService
{
    private readonly ImapSettings _settings = settings.Value;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        { 
            using var client = await ConnectToServerAsync(cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Verbindungstest fehlgeschlagen");
            return false;
        }
    }
    
    public async Task<IList<EmailId>> FetchEmailIdsAsync(string? folderName = null, CancellationToken cancellationToken = default)
    {
        var emails = new List<EmailId>();

        try
        {
            var (client, mailFolder) = await ConnectAndOpenFolderAsync(folderName, cancellationToken);
            using var imapClient = client;
            
            try
            {
                var ids = await mailFolder.SearchAsync(SearchQuery.All, cancellationToken);
                emails.AddRange(ids.Select(ConvertToEmailId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehler beim Abrufen der IDs");
            }

            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Abrufen der E-Mails");
            throw new ImapOperationException("E-Mail-Abruf fehlgeschlagen", ex);
        }

        return emails;
    }

    private static EmailId ConvertToEmailId(UniqueId arg) => new(Validity: arg.Validity, Id: arg.Id);
    
    private async Task<(ImapClient client, IMailFolder mailFolder)> ConnectAndOpenFolderAsync(string? folderName = null, CancellationToken cancellationToken = default)
    {
        ImapClient? client = null;
        try
        {
            client = await ConnectToServerAsync(cancellationToken);
            var folder = string.IsNullOrEmpty(folderName) ? client.Inbox : await client.GetFolderAsync(folderName, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            return (client, folder);
        }
        catch
        {
            client?.Dispose();
            throw;
        }
    }

    public async Task<IList<Email>> FetchEmailsAsync(IEnumerable<EmailId> ids, string? folderName = null, CancellationToken cancellationToken = default)
    {
        var imapIds = Mapping.MapToUniqueIds(ids);
     
        try
        {
            var (client, mailFolder) = await ConnectAndOpenFolderAsync(folderName, cancellationToken);
            using var imapClient = client;

            var mails = LoadMessagesInParallel(client, mailFolder, imapIds);
            
            var emails = mails.Select(
                entry => Mapping.ConvertToEmail(entry.Item1, entry.Item2)).ToList();

            await client.DisconnectAsync(true, cancellationToken);

            return emails;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Abrufen der E-Mails");
            throw new ImapOperationException("E-Mail-Abruf fehlgeschlagen", ex);
        }
    }

    private List<(UniqueId, MimeMessage)> LoadMessagesInParallel(ImapClient client, IMailFolder mailFolder,
        IEnumerable<UniqueId> uids)
    {
        var messages = new List<(UniqueId, MimeMessage)>();
        var lockObject = new object();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _settings.MaxParallelConnections
        };

        Parallel.ForEach(uids, parallelOptions, uid =>
        {
            try
            {
                var message = client.Inbox.GetMessage(uid);
                lock (lockObject)
                {
                    messages.Add((uid, message));
                }
            }
            catch
            {
                // Handle individual message fetch errors
            }
        });

        return messages;
    }

    
    private async Task<ImapClient> ConnectToServerAsync(CancellationToken cancellationToken = default)
    {
        var client = new ImapClient();

        try
        {
            await client.ConnectAsync(_settings.Server, _settings.Port, _settings.UseSsl, cancellationToken);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

            logger.LogInformation("Erfolgreich mit IMAP-Server verbunden: {Server}", _settings.Server);
            return client;
        }
        catch (Exception ex)
        {
            await client.DisconnectAsync(true, cancellationToken);
            throw new ImapConnectionException("Verbindung zum IMAP-Server fehlgeschlagen", ex);
        }
    }


    public async Task<IList<IMailFolder>> GetAllPersonalFoldersAsync(CancellationToken cancellationToken = default)
    {
        using var client = await ConnectToServerAsync(cancellationToken);
        var namespaces = client.PersonalNamespaces;

        var tasks = namespaces.Select(ns => client.GetFoldersAsync(ns, cancellationToken: cancellationToken));
        var results = await Task.WhenAll(tasks);

        await client.DisconnectAsync(true, cancellationToken);

        return results.SelectMany(folders => folders).ToList();
    }
}