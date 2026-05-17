using System.Collections;

namespace PopfileNet.Ui.Services;

public class BucketInfoDto(string id, string name)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
}
