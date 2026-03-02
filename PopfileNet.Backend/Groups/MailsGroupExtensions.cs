using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Database;

namespace PopfileNet.Backend.Groups;

/// <summary>
/// Provides API endpoints for emails and folders.
/// </summary>
public static class MailsGroupExtensions
{
    /// <summary>
    /// Maps the mails and folders endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication AddMailsGroup(this WebApplication app)
    {
        var group = app.MapGroup("/mails");
        
        group.MapGet("/", GetMailsAsync);
        group.MapGet("/{id}", GetMailByIdAsync);
        
        var foldersGroup = app.MapGroup("/folders");
        foldersGroup.MapGet("/", GetFoldersAsync);
        
        return app;
    }

    private static async Task<Ok<PagedApiResponse<EmailDto>>> GetMailsAsync(PopfileNetDbContext db, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        
        try
        {
            var totalCount = await db.Emails.CountAsync();
            var emails = await db.Emails
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmailDto(e.Id, e.Subject, e.FromAddress, e.ReceivedDate, ""))
                .ToListAsync();
            
            return TypedResults.Ok(PagedApiResponse<EmailDto>.Success(emails, page, pageSize, totalCount));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(PagedApiResponse<EmailDto>.Failure("MAILS_ERROR", ex.Message));
        }
    }

    private static async Task<Ok<ApiResponse<EmailDetailDto>>> GetMailByIdAsync(string id, PopfileNetDbContext db)
    {
        try
        {
            var email = await db.Emails.FindAsync(id);
            if (email == null)
                return TypedResults.Ok(ApiResponse<EmailDetailDto>.Failure("NOT_FOUND", "Email not found"));
            
            var result = new EmailDetailDto(
                email.Id,
                email.Subject,
                email.FromAddress,
                email.ToAddresses,
                email.ReceivedDate,
                email.Body,
                ""
            );
            return TypedResults.Ok(ApiResponse<EmailDetailDto>.Success(result));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(ApiResponse<EmailDetailDto>.Failure("MAIL_ERROR", ex.Message));
        }
    }

    private static async Task<Ok<PagedApiResponse<FolderDto>>> GetFoldersAsync(PopfileNetDbContext db, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 100);
        
        try
        {
            var totalCount = await db.MailFolders.CountAsync();
            var folders = await db.MailFolders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FolderDto(f.Id, f.Name))
                .ToListAsync();
            
            return TypedResults.Ok(PagedApiResponse<FolderDto>.Success(folders, page, pageSize, totalCount));
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Ok(PagedApiResponse<FolderDto>.Failure("FOLDERS_ERROR", ex.Message));
        }
    }
}
