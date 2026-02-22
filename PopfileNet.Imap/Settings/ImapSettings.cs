namespace PopfileNet.Imap.Settings;

public class ImapSettings
{
    public required string Server { get; init; }
    public int Port { get; init; } = 993;
    public required string Username { get; init; }
    public required string Password { get; init; }
    public bool UseSsl { get; init; } = true;
    
    public int MaxParallelConnections { get; init; } = 4;
}
