using Microsoft.AspNetCore.Http.HttpResults;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;

namespace PopfileNet.Backend.Groups;

/// <summary>
/// Provides API endpoints for mail accounts.
/// </summary>
public static class AccountsGroupExtensions
{
    /// <summary>
    /// Maps the accounts endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication AddAccountsGroup(this WebApplication app)
    {
        var group = app.MapGroup("/accounts");
        group.MapGet("/", GetAccountsAsync);
        return app;
    }

    private static async Task<IResult> GetAccountsAsync(ISettingsService settingsService, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        
        var settings = await settingsService.GetImapSettingsOnlyAsync();
        
        if (string.IsNullOrEmpty(settings.Server) || string.IsNullOrEmpty(settings.Username))
        {
            return TypedResults.NotFound(PagedApiResponse<AccountDto>.Failure("NO_ACCOUNT", "No IMAP account configured"));
        }
        
        var accounts = new AccountDto(
            "default",
            settings.Username,
            settings.Server,
            settings.Port,
            settings.UseSsl
        );
        
        return TypedResults.Ok(PagedApiResponse<AccountDto>.Success([accounts], page, pageSize, 1));
    }
}
