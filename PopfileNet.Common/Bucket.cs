namespace PopfileNet.Common;
using System.Collections.Generic;

public class Bucket : IBucket
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<MailFolder> Folders { get; set; } = new List<MailFolder>();
}