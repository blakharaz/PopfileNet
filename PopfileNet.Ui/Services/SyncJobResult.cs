namespace PopfileNet.Ui.Services;

public record SyncJobResult(bool Success, string Message, int SyncedCount)
{
    public static SyncJobResult Empty => new(false, "", 0);
}
