namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents IMAP server settings.
/// </summary>
public record ImapSettingsDto(
    string Server = "",
    int Port = 993,
    string Username = "",
    string Password = "",
    bool UseSsl = true,
    int MaxParallelConnections = 4);
