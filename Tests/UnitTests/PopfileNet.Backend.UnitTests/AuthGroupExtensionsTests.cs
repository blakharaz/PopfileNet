using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Models;
using PopfileNet.Common;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public class AuthGroupExtensionsTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<Program>> _loggerMock;

    public AuthGroupExtensionsTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<Program>>();
    }

    [Fact]
    public async Task LoginAsync_ValidRequest_ReturnsSuccess()
    {
        var request = new LoginRequest("user@test.com", "password");
        var userInfo = new UserInfo("1", "user@test.com", "Admin");
        _authServiceMock.Setup(m => m.LoginAsync("user@test.com", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResult(true, userInfo, null));

        var result = await AuthGroupExtensions.LoginAsync(request, _authServiceMock.Object, _loggerMock.Object);

        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<LoginResponse>>>();
        var response = okResult.Value.ShouldNotBeNull();
        response.Value.ShouldNotBeNull().Success.ShouldBeTrue();
        response.Value.User.ShouldNotBeNull();
        response.Value.User.Email.ShouldBe("user@test.com");
    }

    [Fact]
    public async Task LoginAsync_EmptyEmail_ReturnsBadRequest()
    {
        var request = new LoginRequest("", "password");

        var result = await AuthGroupExtensions.LoginAsync(request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<LoginResponse>>>();
    }

    [Fact]
    public async Task LoginAsync_EmptyPassword_ReturnsBadRequest()
    {
        var request = new LoginRequest("user@test.com", "");

        var result = await AuthGroupExtensions.LoginAsync(request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<LoginResponse>>>();
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsUnauthorized()
    {
        var request = new LoginRequest("user@test.com", "wrong");
        _authServiceMock.Setup(m => m.LoginAsync("user@test.com", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResult(false, null, "Invalid email or password"));

        var result = await AuthGroupExtensions.LoginAsync(request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task LogoutAsync_ReturnsOk()
    {
        var result = await AuthGroupExtensions.LogoutAsync(_authServiceMock.Object);

        _authServiceMock.Verify(m => m.LogoutAsync(It.IsAny<CancellationToken>()), Times.Once);
        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<bool>>>();
        okResult.Value!.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCurrentUserAsync_Authenticated_ReturnsUser()
    {
        var userInfo = new UserInfo("1", "user@test.com", "Admin");
        _authServiceMock.Setup(m => m.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        var result = await AuthGroupExtensions.GetCurrentUserAsync(_authServiceMock.Object);

        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<UserDto>>>();
        okResult.Value.ShouldNotBeNull().Value.ShouldNotBeNull().Email.ShouldBe("user@test.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_Unauthenticated_ReturnsUnauthorized()
    {
        _authServiceMock.Setup(m => m.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo?)null);

        var result = await AuthGroupExtensions.GetCurrentUserAsync(_authServiceMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsPagedUsers()
    {
        var users = new List<UserInfo>
        {
            new("1", "user1@test.com", "Admin"),
            new("2", "user2@test.com", "User"),
        };
        _authServiceMock.Setup(m => m.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await AuthGroupExtensions.GetUsersAsync(_authServiceMock.Object, 1, 10);

        result.Value.ShouldNotBeNull().Items.Count().ShouldBe(2);
        result.Value.Items.First().Email.ShouldBe("user1@test.com");
    }

    [Fact]
    public async Task GetUserByIdAsync_Found_ReturnsUser()
    {
        var userInfo = new UserInfo("1", "user@test.com", "Admin");
        _authServiceMock.Setup(m => m.GetUserByIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        var result = await AuthGroupExtensions.GetUserByIdAsync("1", _authServiceMock.Object);

        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<UserDto>>>();
        okResult.Value.ShouldNotBeNull().Value.ShouldNotBeNull().Id.ShouldBe("1");
    }

    [Fact]
    public async Task GetUserByIdAsync_NotFound_ReturnsNotFound()
    {
        _authServiceMock.Setup(m => m.GetUserByIdAsync("999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo?)null);

        var result = await AuthGroupExtensions.GetUserByIdAsync("999", _authServiceMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
    }

    [Fact]
    public async Task CreateUserAsync_ValidRequest_ReturnsCreated()
    {
        var request = new CreateUserRequest("new@test.com", "password123", "User");
        var userInfo = new UserInfo("new-id", "new@test.com", "User");
        _authServiceMock.Setup(m => m.CreateUserAsync("new@test.com", "password123", "User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        var result = await AuthGroupExtensions.CreateUserAsync(request, _authServiceMock.Object, _loggerMock.Object);

        var createdResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<ApiResponse<UserDto>>>();
        createdResult.Value.ShouldNotBeNull().Value.ShouldNotBeNull().Email.ShouldBe("new@test.com");
    }

    [Fact]
    public async Task CreateUserAsync_MissingFields_ReturnsBadRequest()
    {
        var request = new CreateUserRequest("", "password", "User");

        var result = await AuthGroupExtensions.CreateUserAsync(request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<UserDto>>>();
    }

    [Fact]
    public async Task CreateUserAsync_ServiceThrows_ReturnsBadRequest()
    {
        var request = new CreateUserRequest("new@test.com", "password123", "User");
        _authServiceMock.Setup(m => m.CreateUserAsync("new@test.com", "password123", "User", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        var result = await AuthGroupExtensions.CreateUserAsync(request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<UserDto>>>();
    }

    [Fact]
    public async Task UpdateUserAsync_ValidRequest_ReturnsOk()
    {
        var request = new UpdateUserRequest("updated@test.com", "Admin");
        var userInfo = new UserInfo("1", "updated@test.com", "Admin");
        _authServiceMock.Setup(m => m.UpdateUserAsync("1", "updated@test.com", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        var result = await AuthGroupExtensions.UpdateUserAsync("1", request, _authServiceMock.Object, _loggerMock.Object);

        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<UserDto>>>();
        okResult.Value.ShouldNotBeNull().Value.ShouldNotBeNull().Email.ShouldBe("updated@test.com");
    }

    [Fact]
    public async Task UpdateUserAsync_NotFound_ReturnsNotFound()
    {
        var request = new UpdateUserRequest("updated@test.com", "Admin");
        _authServiceMock.Setup(m => m.UpdateUserAsync("999", "updated@test.com", "Admin", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        var result = await AuthGroupExtensions.UpdateUserAsync("999", request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
    }

    [Fact]
    public async Task DeleteUserAsync_ValidId_ReturnsNoContent()
    {
        _authServiceMock.Setup(m => m.DeleteUserAsync("1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await AuthGroupExtensions.DeleteUserAsync("1", _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }

    [Fact]
    public async Task DeleteUserAsync_NotFound_ReturnsNotFound()
    {
        _authServiceMock.Setup(m => m.DeleteUserAsync("999", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        var result = await AuthGroupExtensions.DeleteUserAsync("999", _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
    }

    [Fact]
    public async Task LoginAsync_WhitespaceEmail_ReturnsBadRequest()
    {
        var request = new LoginRequest("   ", "password");

        var result = await AuthGroupExtensions.LoginAsync(request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<LoginResponse>>>();
    }

    [Fact]
    public async Task LoginAsync_WhitespacePassword_ReturnsBadRequest()
    {
        var request = new LoginRequest("user@test.com", "   ");

        var result = await AuthGroupExtensions.LoginAsync(request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<LoginResponse>>>();
    }

    [Fact]
    public async Task CreateUserAsync_WhitespaceRole_ReturnsBadRequest()
    {
        var request = new CreateUserRequest("new@test.com", "password", "   ");

        var result = await AuthGroupExtensions.CreateUserAsync(request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<UserDto>>>();
    }

    [Fact]
    public async Task UpdateUserAsync_ServiceThrows_ReturnsBadRequest()
    {
        var request = new UpdateUserRequest("updated@test.com", "Admin");
        _authServiceMock.Setup(m => m.UpdateUserAsync("1", "updated@test.com", "Admin", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Update failed"));

        var result = await AuthGroupExtensions.UpdateUserAsync("1", request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<UserDto>>>();
    }

    [Fact]
    public async Task DeleteUserAsync_ServiceThrows_ReturnsBadRequest()
    {
        _authServiceMock.Setup(m => m.DeleteUserAsync("1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Delete failed"));

        var result = await AuthGroupExtensions.DeleteUserAsync("1", _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<bool>>>();
    }

    [Fact]
    public async Task GetUsersAsync_CapsPageSize_At100()
    {
        var users = new List<UserInfo> { new("1", "user@test.com", "User") };
        _authServiceMock.Setup(m => m.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var result = await AuthGroupExtensions.GetUsersAsync(_authServiceMock.Object, 1, 200);

        result.Value.ShouldNotBeNull().PageSize.ShouldBe(100);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsSecondPage()
    {
        var users = new List<UserInfo>
        {
            new("1", "user1@test.com", "User"),
            new("2", "user2@test.com", "User"),
            new("3", "user3@test.com", "User"),
        };
        _authServiceMock.Setup(m => m.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var result = await AuthGroupExtensions.GetUsersAsync(_authServiceMock.Object, 2, 2);

        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<PagedApiResponse<UserDto>>>();
        var response = okResult.Value.ShouldNotBeNull();
        response.Items.ShouldNotBeNull().Count().ShouldBe(1);
        response.Items.First().Email.ShouldBe("user3@test.com");
        response.Page.ShouldBe(2);
        response.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task LoginAsync_SuccessWithNullUser_ReturnsSuccessWithNullDto()
    {
        var request = new LoginRequest("user@test.com", "password");
        _authServiceMock.Setup(m => m.LoginAsync("user@test.com", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResult(true, null, null));

        var result = await AuthGroupExtensions.LoginAsync(request, _authServiceMock.Object, _loggerMock.Object);

        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<LoginResponse>>>();
        var response = okResult.Value.ShouldNotBeNull().Value.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
        response.User.ShouldBeNull();
    }

    [Fact]
    public async Task LoginAsync_Exception_ReturnsInternalServerError()
    {
        var request = new LoginRequest("user@test.com", "password");
        _authServiceMock.Setup(m => m.LoginAsync("user@test.com", "password", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var result = await AuthGroupExtensions.LoginAsync(request, _authServiceMock.Object, _loggerMock.Object);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.InternalServerError<ApiResponse<LoginResponse>>>();
    }

    [Fact]
    public async Task UpdateUserAsync_NullEmailAndRole_ReturnsOk()
    {
        var request = new UpdateUserRequest(null, null);
        var userInfo = new UserInfo("1", "user@test.com", "Admin");
        _authServiceMock.Setup(m => m.UpdateUserAsync("1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);

        var result = await AuthGroupExtensions.UpdateUserAsync("1", request, _authServiceMock.Object, _loggerMock.Object);

        var okResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<UserDto>>>();
        okResult.Value.ShouldNotBeNull().Value.ShouldNotBeNull().Id.ShouldBe("1");
    }

    [Fact]
    public async Task DeleteUserAsync_Exception_ReturnsBadRequest()
    {
        _authServiceMock.Setup(m => m.DeleteUserAsync("1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete admin"));

        var result = await AuthGroupExtensions.DeleteUserAsync("1", _authServiceMock.Object, _loggerMock.Object);

        var badResult = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<bool>>>();
        badResult.Value!.Error.ShouldNotBeNull();
        badResult.Value.Error!.Code.ShouldBe("ERROR");
    }
}
