namespace PopfileNet.Common;

public class MailFolder : IMailFolder
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Bucket? Bucket { get; set; }
}