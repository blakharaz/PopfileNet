namespace PopfileNet.Backend.Models;

/// <summary>
/// Represents a mail account.
/// </summary>
public record AccountDto(string Id, string Name, string Server, int Port, bool UseSsl);
