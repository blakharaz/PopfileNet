using Microsoft.AspNetCore.Http.HttpResults;
using PopfileNet.Backend.Models;
using PopfileNet.Imap.Settings;

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

    private static IResult GetAccountsAsync(IConfiguration config, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        
        var imapSettings = config.GetSection("ImapSettings").Get<ImapSettings>();
        
        if (imapSettings?.Server == null || imapSettings?.Username == null)
        {
            return TypedResults.NotFound(PagedApiResponse<AccountDto>.Failure("NO_ACCOUNT", "No IMAP account configured"));
        }
        
        var accounts = new AccountDto(
            "default",
            imapSettings.Username,
            imapSettings.Server,
            imapSettings.Port,
            imapSettings.UseSsl
        );
        
        return TypedResults.Ok(PagedApiResponse<AccountDto>.Success([accounts], page, pageSize, 1));
    }
}
