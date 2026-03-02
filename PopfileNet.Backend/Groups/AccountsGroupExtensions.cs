using Microsoft.AspNetCore.Http.HttpResults;
using PopfileNet.Backend.Models;

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

    private static Ok<PagedApiResponse<AccountDto>> GetAccountsAsync(int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        
        try
        {
            var accounts = new List<AccountDto>
            {
                new("default", "Default Account", "imap.example.com", 993, true)
            };
            
            return TypedResults.Ok(PagedApiResponse<AccountDto>.Success(accounts, page, pageSize, accounts.Count));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(PagedApiResponse<AccountDto>.Failure("ACCOUNTS_ERROR", ex.Message));
        }
    }
}
