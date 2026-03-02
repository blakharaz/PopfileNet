namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents a bucket (classification category).
/// </summary>
public record BucketDto(Guid Id, string Name, string Description);
