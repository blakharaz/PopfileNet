namespace PopfileNet.Common;

public class Bucket : IBucket
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MailFolder? AssociatedFolder { get; set; }
}