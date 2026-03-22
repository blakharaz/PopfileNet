namespace PopfileNet.Imap.Settings;

public record ImapSettings
{
    public required string Server { get; init; }
    public int Port { get; init; } = 993;
    public required string Username { get; init; }
    public required string Password { get; init; }
    public bool UseSsl { get; init; } = true;
    
    private int _maxParallelConnections = 4;
    public int MaxParallelConnections
    {
        get => _maxParallelConnections;
        init => _maxParallelConnections = Math.Clamp(value, 1, 20);
    }
}
