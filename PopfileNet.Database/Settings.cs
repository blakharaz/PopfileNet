namespace PopfileNet.Database;

public class Settings
{
    public int Id { get; set; } = 1;
    public string ImapServer { get; set; } = "";
    public int ImapPort { get; set; } = 993;
    public string ImapUsername { get; set; } = "";
    public string ImapPassword { get; set; } = "";
    public bool ImapUseSsl { get; set; } = true;
    public int MaxParallelConnections { get; set; } = 4;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
