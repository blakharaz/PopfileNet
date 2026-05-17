namespace PopfileNet.Common;

public record UserInfo(string Id, string Email, string Role);

public record LoginResult(bool Success, UserInfo? User, string? Error);

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
    Task<UserInfo?> GetCurrentUserAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UserInfo>> GetUsersAsync(CancellationToken ct = default);
    Task<UserInfo?> GetUserByIdAsync(string id, CancellationToken ct = default);
    Task<UserInfo> CreateUserAsync(string email, string password, string role, CancellationToken ct = default);
    Task<UserInfo> UpdateUserAsync(string id, string? email, string? role, CancellationToken ct = default);
    Task DeleteUserAsync(string id, CancellationToken ct = default);
    Task<bool> AnyUserExistsAsync(CancellationToken ct = default);
}
