namespace PopfileNet.Ui.Services;

public record LoginResponse(bool Success, UserDto? User, string? Error);
