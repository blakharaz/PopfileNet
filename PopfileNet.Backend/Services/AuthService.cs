using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Common;
using PopfileNet.Database;

namespace PopfileNet.Backend.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IHttpContextAccessor httpContextAccessor) : IAuthService
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return new LoginResult(false, null, "Invalid email or password");
        }

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return new LoginResult(false, null, result.IsLockedOut ? "Account is locked" : "Invalid email or password");
        }

        var userInfo = await ToUserInfoAsync(user);
        return new LoginResult(true, userInfo, null);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        await signInManager.SignOutAsync();
    }

    public async Task<UserInfo?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity is not { IsAuthenticated: true } identity)
        {
            return null;
        }

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null)
        {
            return null;
        }

        return await ToUserInfoAsync(user);
    }

    public async Task<IReadOnlyList<UserInfo>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await userManager.Users.ToListAsync(ct);
        var userInfos = new List<UserInfo>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? UserRole;
            userInfos.Add(new UserInfo(user.Id, user.Email ?? "", role));
        }

        return userInfos;
    }

    public async Task<UserInfo?> GetUserByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        return await ToUserInfoAsync(user);
    }

    public async Task<UserInfo> CreateUserAsync(string email, string password, string role, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return await ToUserInfoAsync(user);
    }

    public async Task<UserInfo> UpdateUserAsync(string id, string? email, string? role, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"User with id {id} not found");

        if (!string.IsNullOrEmpty(email) && email != user.Email)
        {
            user.Email = email;
            user.UserName = email;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update user: {errors}");
            }
        }

        if (!string.IsNullOrEmpty(role))
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, role);
        }

        return await ToUserInfoAsync(user);
    }

    public async Task DeleteUserAsync(string id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id)
            ?? throw new KeyNotFoundException($"User with id {id} not found");

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to delete user: {errors}");
        }
    }

    public async Task<bool> AnyUserExistsAsync(CancellationToken ct = default)
    {
        return await userManager.Users.AnyAsync(ct);
    }

    private async Task<UserInfo> ToUserInfoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? UserRole;
        return new UserInfo(user.Id, user.Email ?? "", role);
    }
}
