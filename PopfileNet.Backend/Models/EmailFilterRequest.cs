using PopfileNet.Common;

namespace PopfileNet.Backend.Models;

/// <summary>
/// Request for fetching emails with optional folder filter.
/// </summary>
public sealed class EmailFilterRequest(string? FolderFilter)
{
    public string? FolderFilter { get; } = FolderFilter;
}
