namespace PopfileNet.Common;

public class MailHeader : IMailHeader
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string EmailId { get; set; }
    public Email? Email { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}