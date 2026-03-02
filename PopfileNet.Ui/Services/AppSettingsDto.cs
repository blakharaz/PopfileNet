namespace PopfileNet.Ui.Services;

public record AppSettingsDto
{
    public ImapSettingsDto? ImapSettings { get; init; }
    public List<BucketDto> Buckets { get; init; } = [];
    public List<FolderMappingDto> FolderMappings { get; init; } = [];
}
