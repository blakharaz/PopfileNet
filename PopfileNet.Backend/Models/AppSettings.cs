namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents application settings containing IMAP and bucket configuration.
/// </summary>
public class AppSettings
{
    public ImapSettingsDto? ImapSettings { get; set; }
    public List<BucketDto> Buckets { get; set; } = [];
    public List<FolderMappingDto> FolderMappings { get; set; } = [];
}
