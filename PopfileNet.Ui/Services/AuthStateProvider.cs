using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PopfileNet.Ui.Services;

public class AuthStateProvider(IApiClient apiClient) : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private AuthenticationState? _cachedState;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_cachedState != null)
        {
            return _cachedState;
        }

        var user = await apiClient.GetCurrentUserAsync();
        if (user == null)
        {
            _cachedState = new AuthenticationState(_anonymous);
            return _cachedState;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        var identity = new ClaimsIdentity(claims, "cookie");
        var principal = new ClaimsPrincipal(identity);
        _cachedState = new AuthenticationState(principal);
        return _cachedState;
    }

    public void MarkInitialized()
    {
        _cachedState = null;
    }

    public async Task OnLoginSuccessAsync()
    {
        _cachedState = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void OnLogout()
    {
        _cachedState = new AuthenticationState(_anonymous);
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
    }
}
