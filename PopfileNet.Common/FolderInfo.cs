namespace PopfileNet.Common;

public class FolderInfo(string name, int count)
{
    public string Name { get; } = name;
    public int Count { get; } = count;
}
