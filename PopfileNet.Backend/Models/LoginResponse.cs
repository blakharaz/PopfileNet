namespace PopfileNet.Backend.Models;

public record LoginResponse(bool Success, UserDto? User, string? Error);
