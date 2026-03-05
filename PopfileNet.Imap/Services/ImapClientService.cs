using MailKit;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PopfileNet.Common;
using PopfileNet.Imap.Exceptions;
using PopfileNet.Imap.Models;
using PopfileNet.Imap.Settings;
using System.Collections.Concurrent;
using IMailFolder = MailKit.IMailFolder;

namespace PopfileNet.Imap.Services;

public class ImapClientService(
    IOptions<ImapSettings> settings,
    ILogger<ImapClientService> logger,
    IImapClientFactory clientFactory)
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

    private static EmailId ConvertToEmailId(UniqueId arg) => new(validity: arg.Validity, id: arg.Id);
    
    private async Task<(IImapClient client, IMailFolder mailFolder)> ConnectAndOpenFolderAsync(string? folderName = null, CancellationToken cancellationToken = default)
    {
        IImapClient? client = null;
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
        var idList = imapIds.ToList();
        
        if (idList.Count == 0)
        {
            return [];
        }
     
        try
        {
            var mails = await LoadMessagesInParallelAsync(folderName, idList, cancellationToken);
            
            var emails = mails.Select(
                entry => Mapping.ConvertToEmail(entry.Item1, entry.Item2, folderName)).ToList();

            return emails;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Abrufen der E-Mails");
            throw new ImapOperationException("E-Mail-Abruf fehlgeschlagen", ex);
        }
    }

    private async Task<List<(UniqueId, MimeMessage)>> LoadMessagesInParallelAsync(
        string? folderName,
        List<UniqueId> uidList,
        CancellationToken cancellationToken = default)
    {
        if (uidList.Count == 0)
        {
            return [];
        }
        
        var messages = new ConcurrentBag<(UniqueId, MimeMessage)>();
        var clientPool = new ConcurrentQueue<IImapClient>();
        var availableSemaphore = new SemaphoreSlim(_settings.MaxParallelConnections);

        try
        {
            for (int i = 0; i < _settings.MaxParallelConnections; i++)
            {
                clientPool.Enqueue(await ConnectToServerAsync(cancellationToken));
            }

            await FetchAllMessagesAsync(folderName, uidList, messages, clientPool, availableSemaphore, cancellationToken);

            return messages.ToList();
        }
        finally
        {
            while (clientPool.TryDequeue(out var client))
            {
                try
                {
                    await client.DisconnectAsync(true, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Fehler beim Trennen der IMAP-Verbindung");
                }
                client.Dispose();
            }
            availableSemaphore.Dispose();
        }
    }

    private async Task FetchAllMessagesAsync(
        string? folderName,
        List<UniqueId> uidList,
        ConcurrentBag<(UniqueId, MimeMessage)> messages,
        ConcurrentQueue<IImapClient> clientPool,
        SemaphoreSlim availableSemaphore,
        CancellationToken cancellationToken)
    {
        var tasks = uidList.Select(uid =>
            FetchMessageWithPoolAsync(folderName, uid, messages, clientPool, availableSemaphore, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task FetchMessageWithPoolAsync(
        string? folderName,
        UniqueId uid,
        ConcurrentBag<(UniqueId, MimeMessage)> messages,
        ConcurrentQueue<IImapClient> clientPool,
        SemaphoreSlim availableSemaphore,
        CancellationToken cancellationToken)
    {
        await availableSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!clientPool.TryDequeue(out var client))
            {
                logger.LogWarning("No available client in pool for UID {Uid}", uid.Id);
                return;
            }
            
            try
            {
                var folder = string.IsNullOrEmpty(folderName) ? client.Inbox : await client.GetFolderAsync(folderName, cancellationToken);
                if (!folder.IsOpen)
                {
                    folder.Open(FolderAccess.ReadOnly, cancellationToken);
                }

                var message = folder.GetMessage(uid, cancellationToken);
                messages.Add((uid, message));
            }
            finally
            {
                clientPool.Enqueue(client);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehler beim Abrufen der Nachricht mit UID {Uid}", uid.Id);
        }
        finally
        {
            availableSemaphore.Release();
        }
    }

    
    private async Task<IImapClient> ConnectToServerAsync(CancellationToken cancellationToken = default)
    {
        var client = clientFactory.Create();

        try
        {
            await client.ConnectAsync(_settings.Server, _settings.Port, _settings.UseSsl, cancellationToken);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

            logger.LogInformation("Erfolgreich mit IMAP-Server verbunden: {Server}", _settings.Server);
            return client;
        }
        catch (Exception ex)
        {
            try
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true, cancellationToken);
            }
            catch
            {
            }
            finally
            {
                client.Dispose();
            }

            throw new ImapConnectionException("Connecting IMAP server failed", ex);
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
