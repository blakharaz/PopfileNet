namespace PopfileNet.Ui.Services;

public record ImapSettingsDto
{
    public string Server { get; init; } = "";
    public int Port { get; init; } = 993;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public bool UseSsl { get; init; } = true;
    public int MaxParallelConnections { get; init; } = 4;
}
