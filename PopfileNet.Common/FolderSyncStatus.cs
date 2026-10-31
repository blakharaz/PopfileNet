namespace PopfileNet.Common;

/// <summary>
/// Describes the sync state of a mail folder.
/// </summary>
public enum FolderSyncStatus
{
    /// <summary>The folder is not currently being synced.</summary>
    Idle,

    /// <summary>The folder is currently being synced.</summary>
    Syncing,

    /// <summary>The last sync attempt for this folder failed.</summary>
    Error
}
