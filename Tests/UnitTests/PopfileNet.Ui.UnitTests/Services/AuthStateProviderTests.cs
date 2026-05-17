using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using PopfileNet.Ui.Services;
using Shouldly;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Services;

public class AuthStateProviderTests
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly AuthStateProvider _authStateProvider;

    public AuthStateProviderTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _authStateProvider = new AuthStateProvider(_apiClientMock.Object);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_Unauthenticated_ReturnsAnonymous()
    {
        _apiClientMock.Setup(m => m.GetCurrentUserAsync()).ReturnsAsync((UserDto?)null);

        var state = await _authStateProvider.GetAuthenticationStateAsync();

        state.User.Identity?.IsAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_Authenticated_ReturnsClaimsPrincipal()
    {
        var userDto = new UserDto("1", "user@test.com", "Admin");
        _apiClientMock.Setup(m => m.GetCurrentUserAsync()).ReturnsAsync(userDto);
        _authStateProvider.MarkInitialized();

        var state = await _authStateProvider.GetAuthenticationStateAsync();

        state.User.Identity?.IsAuthenticated.ShouldBeTrue();
        state.User.FindFirst(ClaimTypes.Email)?.Value.ShouldBe("user@test.com");
        state.User.FindFirst(ClaimTypes.Role)?.Value.ShouldBe("Admin");
        state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value.ShouldBe("1");
    }

    [Fact]
    public async Task OnLoginSuccessAsync_UpdatesState()
    {
        var userDto = new UserDto("1", "user@test.com", "Admin");
        _apiClientMock.Setup(m => m.GetCurrentUserAsync()).ReturnsAsync(userDto);

        await _authStateProvider.OnLoginSuccessAsync();

        var state = await _authStateProvider.GetAuthenticationStateAsync();
        state.User.Identity?.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task OnLogout_ReturnsAnonymousState()
    {
        _authStateProvider.OnLogout();

        var state = await _authStateProvider.GetAuthenticationStateAsync();

        state.User.Identity?.IsAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public void MarkInitialized_SetsInitializedFlag()
    {
        _authStateProvider.MarkInitialized();

        var state = _authStateProvider.GetAuthenticationStateAsync();
        state.IsCompleted.ShouldBeTrue();
    }
}
