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

     public async Task<IReadOnlyList<FolderMappingDto>> GetFolderMappingsAsync(CancellationToken ct = default)
     {
         var folders = await db.MailFolders.ToListAsync(ct);
         return folders.Select(f => new FolderMappingDto(f.Name, f.BucketId)).ToList();
     }

     public async Task SetFolderMappingAsync(string folderName, string? bucketId, CancellationToken ct = default)
     {
         if (string.IsNullOrWhiteSpace(folderName))
         {
             throw new ArgumentException("Folder name cannot be null or empty", nameof(folderName));
         }

         var folder = await db.MailFolders.FirstOrDefaultAsync(f => f.Name == folderName, ct);
         if (folder == null)
         {
             throw new KeyNotFoundException($"Folder '{folderName}' not found");
         }

         // Validate bucket exists if bucketId is provided
         if (!string.IsNullOrEmpty(bucketId))
         {
             var bucketExists = await db.Buckets.AnyAsync(b => b.Id == bucketId, ct);
             if (!bucketExists)
             {
                 throw new KeyNotFoundException($"Bucket with ID '{bucketId}' not found");
             }
         }

         folder.BucketId = string.IsNullOrEmpty(bucketId) ? null : bucketId;
         await db.SaveChangesAsync(ct);
     }

     public async Task RemoveFolderMappingAsync(string folderName, CancellationToken ct = default)
     {
         if (string.IsNullOrWhiteSpace(folderName))
         {
             throw new ArgumentException("Folder name cannot be null or empty", nameof(folderName));
         }

         var folder = await db.MailFolders.FirstOrDefaultAsync(f => f.Name == folderName, ct);
         if (folder == null)
         {
             throw new KeyNotFoundException($"Folder '{folderName}' not found");
         }

         folder.BucketId = null;
         await db.SaveChangesAsync(ct);
     }
 }
