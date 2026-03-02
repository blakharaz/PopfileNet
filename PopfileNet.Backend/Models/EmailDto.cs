namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents a summary of an email.
/// </summary>
public record EmailDto(string Id, string Subject, string FromAddress, DateTime ReceivedDate, string BucketName);
