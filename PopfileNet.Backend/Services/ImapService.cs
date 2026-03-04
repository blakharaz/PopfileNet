using Microsoft.Extensions.Options;
using PopfileNet.Common;
using PopfileNet.Imap;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Backend.Services;


// simple domain exception used when the backend has not been configured yet
public class ImapNotConfiguredException : Exception
{
    public ImapNotConfiguredException()
        : base("IMAP settings are not configured")
    {
    }
}

public class ImapService(ISettingsService settingsService, ILogger<ImapClientService> logger, IImapClientFactory clientFactory) : IImapService
{
    private static bool HasRequiredSettings(ImapSettings settings)
        => !string.IsNullOrWhiteSpace(settings.Server) && !string.IsNullOrWhiteSpace(settings.Username);

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetImapSettingsAsync(cancellationToken);
        return HasRequiredSettings(settings);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsConfiguredAsync(cancellationToken))
            return false;

        var imapClientService = await GetImapClientService(cancellationToken);
        return await imapClientService.TestConnectionAsync(cancellationToken);
    }

    private async Task<ImapClientService> GetImapClientService(CancellationToken cancellationToken)
    {
        var settings = await GetImapSettingsAsync(cancellationToken);
        
        if (!HasRequiredSettings(settings))
        {
            // throw a more specific exception for callers that bypass configuration checks
            throw new ImapNotConfiguredException();
        }
        
        var imapClientService = new ImapClientService(Options.Create(settings), logger, clientFactory);
        return imapClientService;
    }

    public async Task<IList<EmailId>> FetchEmailIdsAsync(string? folderName = null, CancellationToken cancellationToken = default)
    {
        if (!await IsConfiguredAsync(cancellationToken))
            return new List<EmailId>();

        return await (await GetImapClientService(cancellationToken)).FetchEmailIdsAsync(folderName, cancellationToken);
    }

    public async Task<IList<Email>> FetchEmailsAsync(IEnumerable<EmailId> ids, string? folderName = null, CancellationToken cancellationToken = default)
    {
        if (!await IsConfiguredAsync(cancellationToken))
            return new List<Email>();

        return await (await GetImapClientService(cancellationToken)).FetchEmailsAsync(ids, folderName, cancellationToken);
    }

    public async Task<List<FolderInfo>> GetAllPersonalFoldersAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsConfiguredAsync(cancellationToken))
            return new List<FolderInfo>();

        var folders = await (await GetImapClientService(cancellationToken)).GetAllPersonalFoldersAsync(cancellationToken);
        return folders.Select(f => new FolderInfo(id: f.Id, fullname: f.FullName, name: f.Name)).ToList();
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
