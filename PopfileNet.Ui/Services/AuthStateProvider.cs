using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PopfileNet.Ui.Services;

public class AuthStateProvider(IApiClient apiClient) : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private bool _isInitialized;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_isInitialized)
        {
            _isInitialized = true;
        }

        var user = await apiClient.GetCurrentUserAsync();
        if (user == null)
        {
            return new AuthenticationState(_anonymous);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        var identity = new ClaimsIdentity(claims, "cookie");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }

    public void MarkInitialized()
    {
        _isInitialized = true;
    }

    public async Task OnLoginSuccessAsync()
    {
        _isInitialized = true;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void OnLogout()
    {
        _isInitialized = true;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }
}
