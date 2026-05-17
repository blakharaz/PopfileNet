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
    public async Task LoginAsync_ReturnsDefaultRole_WhenNoRolesAssigned()
    {
        var user = new ApplicationUser { Id = "1", Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        _signInManagerMock.Setup(m => m.PasswordSignInAsync(user, "password", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);

        var result = await _authService.LoginAsync("test@example.com", "password");

        result.Success.ShouldBeTrue();
        result.User.Role.ShouldBe("User");
    }

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
