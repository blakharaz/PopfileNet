using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Database;
using PopfileNet.Imap.Settings;

namespace PopfileNet.Backend.Services;

public class SettingsService(PopfileNetDbContext db, ImapSettings defaults) : ISettingsService
{
    private const int SettingsId = 1;

    public async Task<AppSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var dbSettings = await db.Settings.FindAsync([SettingsId], ct);
        var buckets = await db.Buckets.ToListAsync(ct);
        var folders = await db.MailFolders.ToListAsync(ct);

        var settings = new AppSettings
        {
            ImapSettings = new ImapSettingsDto
            {
                Server = dbSettings?.ImapServer ?? defaults.Server,
                Port = dbSettings?.ImapPort ?? defaults.Port,
                Username = dbSettings?.ImapUsername ?? defaults.Username,
                Password = "",
                UseSsl = dbSettings?.ImapUseSsl ?? defaults.UseSsl,
                MaxParallelConnections = dbSettings?.MaxParallelConnections ?? defaults.MaxParallelConnections
            },
            Buckets = buckets.Select(b => new BucketDto(b.Id, b.Name, b.Description ?? "")).ToList(),
            FolderMappings = folders.Select(f => new FolderMappingDto(f.Name, f.BucketId)).ToList()
        };

        return settings;
    }

    public async Task<ImapSettingsDto> GetImapSettingsOnlyAsync(CancellationToken ct = default)
    {
        var dbSettings = await db.Settings.FindAsync([SettingsId], ct);
        
        return new ImapSettingsDto
        {
            Server = dbSettings?.ImapServer ?? defaults.Server,
            Port = dbSettings?.ImapPort ?? defaults.Port,
            Username = dbSettings?.ImapUsername ?? defaults.Username,
            Password = dbSettings?.ImapPassword ?? defaults.Password,
            UseSsl = dbSettings?.ImapUseSsl ?? defaults.UseSsl,
            MaxParallelConnections = dbSettings?.MaxParallelConnections ?? defaults.MaxParallelConnections
        };
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken ct = default)
    {
        var dbSettings = await db.Settings.FindAsync([SettingsId], ct);

        if (dbSettings == null)
        {
            dbSettings = new Settings { Id = SettingsId };
            db.Settings.Add(dbSettings);
        }

        if (settings.ImapSettings != null)
        {
            dbSettings.ImapServer = settings.ImapSettings.Server ?? "";
            dbSettings.ImapPort = settings.ImapSettings.Port;
            dbSettings.ImapUsername = settings.ImapSettings.Username ?? "";
            if (!string.IsNullOrEmpty(settings.ImapSettings.Password))
            {
                dbSettings.ImapPassword = settings.ImapSettings.Password;
            }
            dbSettings.ImapUseSsl = settings.ImapSettings.UseSsl;
            var maxConn = settings.ImapSettings.MaxParallelConnections;
            dbSettings.MaxParallelConnections = maxConn > 0 && maxConn <= 20 ? maxConn : defaults.MaxParallelConnections;
        }

        dbSettings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
