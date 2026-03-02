namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents detailed information about an email.
/// </summary>
public record EmailDetailDto(
    string Id,
    string Subject,
    string FromAddress,
    string ToAddresses,
    DateTime ReceivedDate,
    string Body,
    string BucketName);
