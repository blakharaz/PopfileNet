using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _signInManagerMock = CreateSignInManagerMock();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _authService = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, "password", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var result = await _authService.LoginAsync("test@example.com", "password");

        result.Success.ShouldBeTrue();
        result.User.ShouldNotBeNull();
        result.User.Email.ShouldBe("test@example.com");
        result.User.Role.ShouldBe("Admin");
        result.Error.ShouldBeNull();
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ReturnsFailure()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await _authService.LoginAsync("wrong@example.com", "password");

        result.Success.ShouldBeFalse();
        result.User.ShouldBeNull();
        result.Error.ShouldNotBeNull();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, "wrong", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await _authService.LoginAsync("test@example.com", "wrong");

        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task LoginAsync_LockedAccount_ReturnsLockedMessage()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, "wrong", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var result = await _authService.LoginAsync("test@example.com", "wrong");

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("Account is locked");
    }

    [Fact]
    public async Task LoginAsync_ReturnsDefaultRole_WhenNoRolesAssigned()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, "password", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        var result = await _authService.LoginAsync("test@example.com", "password");

        result.Success.ShouldBeTrue();
        result.User.ShouldNotBeNull();
        result.User.Role.ShouldBe("User");
    }

    [Fact]
    public async Task LogoutAsync_CallsSignOut()
    {
        await _authService.LogoutAsync();

        _signInManagerMock.Verify(m => m.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserAsync_AuthenticatedUser_ReturnsUserInfo()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@example.com", UserName = "test@example.com" };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Email, "test@example.com")
            },
            "cookie",
            ClaimTypes.NameIdentifier,
            ClaimTypes.Role));

        _httpContextAccessorMock.Setup(m => m.HttpContext).Returns(new DefaultHttpContext { User = claimsPrincipal });
        _userManagerMock.Setup(m => m.GetUserAsync(claimsPrincipal)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var result = await _authService.GetCurrentUserAsync();

        result.ShouldNotBeNull();
        result.Email.ShouldBe("test@example.com");
        result.Role.ShouldBe("Admin");
    }

    [Fact]
    public async Task GetCurrentUserAsync_Unauthenticated_ReturnsNull()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(m => m.HttpContext).Returns(httpContext);

        var result = await _authService.GetCurrentUserAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentUserAsync_UserNotFoundAfterAuth_ReturnsNull()
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
            "cookie",
            ClaimTypes.NameIdentifier,
            ClaimTypes.Role));

        _httpContextAccessorMock.Setup(m => m.HttpContext).Returns(new DefaultHttpContext { User = claimsPrincipal });
        _userManagerMock.Setup(m => m.GetUserAsync(claimsPrincipal)).ReturnsAsync((ApplicationUser?)null);

        var result = await _authService.GetCurrentUserAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_Found_ReturnsUserInfo()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var result = await _authService.GetUserByIdAsync("1");

        result.ShouldNotBeNull();
        result.Id.ShouldBe("1");
        result.Email.ShouldBe("test@example.com");
        result.Role.ShouldBe("Admin");
    }

    [Fact]
    public async Task GetUserByIdAsync_NotFound_ReturnsNull()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

        var result = await _authService.GetUserByIdAsync("999");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateUserAsync_Success_ReturnsUserInfo()
    {
        var createdUser = new ApplicationUser { Id = "new-id", Email = "new@test.com", UserName = "new@test.com" };
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "password"))
            .Callback<ApplicationUser, string>((u, _) =>
            {
                u.Id = "new-id";
                u.Email = "new@test.com";
                u.UserName = "new@test.com";
            })
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(false);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(["User"]);

        var result = await _authService.CreateUserAsync("new@test.com", "password", "User");

        result.Email.ShouldBe("new@test.com");
        result.Role.ShouldBe("User");
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "password"), Times.Once);
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_Failure_ThrowsInvalidOperationException()
    {
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already exists" }));

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _authService.CreateUserAsync("existing@test.com", "password", "User"));

        exception.Message.ShouldContain("Email already exists");
    }

    [Fact]
    public async Task CreateUserAsync_UserAlreadyInRole_DoesNotAddRole()
    {
        var createdUser = new ApplicationUser { Id = "new-id", Email = "new@test.com", UserName = "new@test.com" };
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "password"))
            .Callback<ApplicationUser, string>((u, _) =>
            {
                u.Id = "new-id";
                u.Email = "new@test.com";
                u.UserName = "new@test.com";
            })
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Admin"))
            .ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(["Admin"]);

        await _authService.CreateUserAsync("new@test.com", "password", "Admin");

        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_Success_UpdatesEmailAndRole()
    {
        var user = new ApplicationUser { Id = "1", Email = "old@test.com", UserName = "old@test.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);
        _userManagerMock.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(user, "User")).ReturnsAsync(IdentityResult.Success);

        var result = await _authService.UpdateUserAsync("1", "new@test.com", "User");

        result.Email.ShouldBe("new@test.com");
        user.Email.ShouldBe("new@test.com");
        user.UserName.ShouldBe("new@test.com");
        _userManagerMock.Verify(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        _userManagerMock.Verify(m => m.AddToRoleAsync(user, "User"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => _authService.UpdateUserAsync("999", "new@test.com", "User"));
    }

    [Fact]
    public async Task UpdateUserAsync_UpdateFails_ThrowsInvalidOperationException()
    {
        var user = new ApplicationUser { Id = "1", Email = "old@test.com", UserName = "old@test.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid email" }));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _authService.UpdateUserAsync("1", "invalid-email", "User"));
    }

    [Fact]
    public async Task UpdateUserAsync_OnlyRoleChange_DoesNotUpdateEmail()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@test.com", UserName = "test@test.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);
        _userManagerMock.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);

        var result = await _authService.UpdateUserAsync("1", null, "Admin");

        result.Email.ShouldBe("test@test.com");
        _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_Success_DeletesUser()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@test.com", UserName = "test@test.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _authService.DeleteUserAsync("1");

        _userManagerMock.Verify(m => m.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

        await Should.ThrowAsync<KeyNotFoundException>(
            () => _authService.DeleteUserAsync("999"));
    }

    [Fact]
    public async Task DeleteUserAsync_DeleteFails_ThrowsInvalidOperationException()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@test.com", UserName = "test@test.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Cannot delete" }));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _authService.DeleteUserAsync("1"));
    }

    [Fact]
    public async Task ToUserInfoAsync_HandlesNullEmail()
    {
        var user = new ApplicationUser { Id = "1", Email = null, UserName = "user1" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("user1")).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, "password", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var result = await _authService.LoginAsync("user1", "password");

        result.User.ShouldNotBeNull();
        result.User.Email.ShouldBe("");
    }

    // GetUsersAsync requires EF Core integration testing due to ToListAsync on UserManager.Users

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock()
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var userMgr = CreateUserManagerMock();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<SignInManager<ApplicationUser>>>();
        var schemes = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        var confirmation = new Mock<Microsoft.AspNetCore.Identity.IUserConfirmation<ApplicationUser>>();

        return new Mock<SignInManager<ApplicationUser>>(
            userMgr.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            options.Object,
            logger.Object,
            schemes.Object,
            confirmation.Object);
    }
}
