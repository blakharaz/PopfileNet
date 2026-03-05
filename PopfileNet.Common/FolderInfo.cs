namespace PopfileNet.Common;

public class FolderInfo(string id, string fullname, string name)
{
    public string Name { get; } = name;
    public string FullName { get; } = fullname;
    public string Id { get; } = id;
}
