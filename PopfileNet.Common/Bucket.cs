namespace PopfileNet.Common;

public class Bucket : IBucket
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MailFolder? AssociatedFolder { get; set; }
}