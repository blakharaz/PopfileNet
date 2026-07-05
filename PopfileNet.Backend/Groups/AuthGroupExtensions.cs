using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using PopfileNet.Backend.Models;
using PopfileNet.Common;

namespace PopfileNet.Backend.Groups;

public static class AuthGroupExtensions
{
    private const string Error = "ERROR";

    public static WebApplication AddAuthGroup(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapGet("/me", GetCurrentUserAsync);

        var adminGroup = group.MapGroup("").RequireAuthorization(Program.AdminRole);
        adminGroup.MapGet("/users", GetUsersAsync);
        adminGroup.MapGet("/users/{id}", GetUserByIdAsync);
        adminGroup.MapPost("/users", CreateUserAsync);
        adminGroup.MapPut("/users/{id}", UpdateUserAsync);
        adminGroup.MapDelete("/users/{id}", DeleteUserAsync);

        return app;
    }

    internal static async Task<IResult> LoginAsync(LoginRequest request, IAuthService authService, ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return TypedResults.BadRequest(ApiResponse<LoginResponse>.Failure("INVALID_INPUT", "Email and password are required"));
        }

        try
        {
            var result = await authService.LoginAsync(request.Email, request.Password);

            if (!result.Success)
            {
                return TypedResults.Unauthorized();
            }

            var response = new LoginResponse(true, result.User != null ? new UserDto(result.User.Id, result.User.Email, result.User.Role) : null, null);
            return TypedResults.Ok(ApiResponse<LoginResponse>.Success(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login");
            return TypedResults.InternalServerError(ApiResponse<LoginResponse>.Failure(Error, "An unexpected error occurred"));
        }
    }

    internal static async Task<IResult> LogoutAsync(IAuthService authService)
    {
        await authService.LogoutAsync();
        return TypedResults.Ok(ApiResponse<bool>.Success(true));
    }

    internal static async Task<IResult> GetCurrentUserAsync(IAuthService authService)
    {
        var user = await authService.GetCurrentUserAsync();
        if (user == null)
        {
            return TypedResults.Unauthorized();
        }

        var dto = new UserDto(user.Id, user.Email, user.Role);
        return TypedResults.Ok(ApiResponse<UserDto>.Success(dto));
    }

    internal static async Task<Ok<PagedApiResponse<UserDto>>> GetUsersAsync(IAuthService authService, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        var users = await authService.GetUsersAsync();
        var totalCount = users.Count;
        var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).Select(u => new UserDto(u.Id, u.Email, u.Role)).ToList();

        return TypedResults.Ok(PagedApiResponse<UserDto>.Success(pagedUsers, page, pageSize, totalCount));
    }

    internal static async Task<IResult> GetUserByIdAsync(string id, IAuthService authService)
    {
        var user = await authService.GetUserByIdAsync(id);
        if (user == null)
        {
            return TypedResults.NotFound();
        }

        var dto = new UserDto(user.Id, user.Email, user.Role);
        return TypedResults.Ok(ApiResponse<UserDto>.Success(dto));
    }

    internal static async Task<IResult> CreateUserAsync(CreateUserRequest request, IAuthService authService, ILogger<Program> logger)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Role))
        {
            return TypedResults.BadRequest(ApiResponse<UserDto>.Failure("INVALID_INPUT", "Email, password, and role are required"));
        }

        try
        {
            var user = await authService.CreateUserAsync(request.Email, request.Password, request.Role);
            var dto = new UserDto(user.Id, user.Email, user.Role);
            return TypedResults.Created($"/auth/users/{user.Id}", ApiResponse<UserDto>.Success(dto));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to create user");
            return TypedResults.BadRequest(ApiResponse<UserDto>.Failure(Error, ex.Message));
        }
    }

    internal static async Task<IResult> UpdateUserAsync(string id, UpdateUserRequest request, IAuthService authService, ILogger<Program> logger)
    {
        try
        {
            var user = await authService.UpdateUserAsync(id, request.Email, request.Role);
            var dto = new UserDto(user.Id, user.Email, user.Role);
            return TypedResults.Ok(ApiResponse<UserDto>.Success(dto));
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to update user {Id}", id);
            return TypedResults.BadRequest(ApiResponse<UserDto>.Failure(Error, ex.Message));
        }
    }

    internal static async Task<IResult> DeleteUserAsync(string id, IAuthService authService, ILogger<Program> logger)
    {
        try
        {
            await authService.DeleteUserAsync(id);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to delete user {Id}", id);
            return TypedResults.BadRequest(ApiResponse<bool>.Failure(Error, ex.Message));
        }
    }
}
