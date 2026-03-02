namespace PopfileNet.Common;

public class Email : IEmail
{
    public EmailId? UniqueId { get; set; }
    public string Id { get; init; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddresses { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public bool IsHtml { get; set; }
    public IList<MailHeader> Headers { get; set; } = [];
    public Guid Folder { get; set; } = Guid.Empty;
    public MailFolder? FolderNavigation { get; set; }
}