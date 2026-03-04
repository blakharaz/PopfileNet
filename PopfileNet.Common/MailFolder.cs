namespace PopfileNet.Common;

public class MailFolder : IMailFolder
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? BucketId { get; set; }
    public Bucket? Bucket { get; set; }
}