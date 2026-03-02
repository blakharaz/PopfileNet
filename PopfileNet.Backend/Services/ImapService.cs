using Microsoft.Extensions.Options;
using PopfileNet.Common;
using PopfileNet.Imap;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Backend.Services;

public class ImapService(ISettingsService settingsService, ILogger<ImapClientService> logger, IImapClientFactory clientFactory) : IImapService
{
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var imapClientService = await GetImapClientService(cancellationToken);
        return await imapClientService.TestConnectionAsync(cancellationToken);
    }

    private async Task<ImapClientService> GetImapClientService(CancellationToken cancellationToken)
    {
        var settings = await GetImapSettingsAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(settings.Server) || string.IsNullOrEmpty(settings.Username))
        {
            throw new InvalidDataException("no server or no username set for IMAP");
        }
        
        var imapClientService = new ImapClientService(Options.Create(settings), logger, clientFactory);
        return imapClientService;
    }

    public async Task<IList<EmailId>> FetchEmailIdsAsync(string? folderName = null, CancellationToken cancellationToken = default) =>
        await (await GetImapClientService(cancellationToken)).FetchEmailIdsAsync(folderName, cancellationToken);

    public async Task<IList<Email>> FetchEmailsAsync(IEnumerable<EmailId> ids, string? folderName = null, CancellationToken cancellationToken = default) =>
        await (await GetImapClientService(cancellationToken)).FetchEmailsAsync(ids, folderName, cancellationToken);

    public async Task<List<FolderInfo>> GetAllPersonalFoldersAsync(CancellationToken cancellationToken = default)
    {
        var folders = await (await GetImapClientService(cancellationToken)).GetAllPersonalFoldersAsync(cancellationToken);
        return folders.Select(f => new FolderInfo(f.Name, f.Count)).ToList();
    }

    private async Task<ImapSettings> GetImapSettingsAsync(CancellationToken ct = default)
    {
        var imapSettings = await settingsService.GetImapSettingsOnlyAsync(ct);
        return new ImapSettings
        {
            Server = imapSettings.Server,
            Port = imapSettings.Port,
            Username = imapSettings.Username,
            Password = imapSettings.Password,
            UseSsl = imapSettings.UseSsl,
            MaxParallelConnections = imapSettings.MaxParallelConnections
        };
    }
}
