namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents a mapping between a folder and a bucket.
/// </summary>
public record FolderMappingDto(string FolderName, Guid? BucketId);
