using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Database;

namespace PopfileNet.Backend.Services;

public class SettingsService(PopfileNetDbContext db) : ISettingsService
{
    public async Task<AppSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var dbSettings = await db.Settings.FirstOrDefaultAsync(ct);
        var buckets = await db.Buckets.ToListAsync(ct);
        var folders = await db.MailFolders.ToListAsync(ct);

        var settings = new AppSettings
        {
            ImapSettings = new ImapSettingsDto
            {
                Server = dbSettings?.ImapServer ?? "",
                Port = dbSettings?.ImapPort ?? 993,
                Username = dbSettings?.ImapUsername ?? "",
                Password = dbSettings?.ImapPassword ?? "",
                UseSsl = dbSettings?.ImapUseSsl ?? true,
                MaxParallelConnections = dbSettings?.MaxParallelConnections ?? 4
            },
            Buckets = buckets.Select(b => new BucketDto(b.Id, b.Name, b.Description ?? "")).ToList(),
            FolderMappings = folders.Select(f => new FolderMappingDto(f.Name, f.BucketId)).ToList()
        };

        return settings;
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken ct = default)
    {
        var dbSettings = await db.Settings.FirstOrDefaultAsync(ct);

        if (dbSettings == null)
        {
            dbSettings = new Database.Settings { Id = 1 };
            db.Settings.Add(dbSettings);
        }

        if (settings.ImapSettings != null)
        {
            dbSettings.ImapServer = settings.ImapSettings.Server;
            dbSettings.ImapPort = settings.ImapSettings.Port;
            dbSettings.ImapUsername = settings.ImapSettings.Username;
            dbSettings.ImapPassword = settings.ImapSettings.Password;
            dbSettings.ImapUseSsl = settings.ImapSettings.UseSsl;
            dbSettings.MaxParallelConnections = settings.ImapSettings.MaxParallelConnections;
        }

        dbSettings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
