namespace PopfileNet.Common;

public interface IMailFolder
{
    public Guid Id { get; init; }
    public string Name { get; set; }
    public Bucket? Bucket { get; set; }
}