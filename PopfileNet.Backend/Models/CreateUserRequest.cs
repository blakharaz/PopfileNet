namespace PopfileNet.Backend.Models;

public record CreateUserRequest(string Email, string Password, string Role);
