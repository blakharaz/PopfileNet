using Microsoft.EntityFrameworkCore;
using Npgsql;
using PopfileNet.Common;

namespace PopfileNet.Database.Repositories;

public class EmailRepository(PopfileNetDbContext dbContext) : IEmailRepository
{
    public async Task<int> InsertEmailsIgnoringDuplicatesAsync(IEnumerable<Email> emails, CancellationToken ct = default)
    {
        var emailList = emails.ToList();
        if (emailList.Count == 0)
            return 0;

        var sql = """
            INSERT INTO "Emails" ("Id", "ImapUid", "Subject", "FromAddress", "ToAddresses", "Body", "ReceivedDate", "IsHtml", "Folder", "Validity", "UniqueId")
            VALUES (@Id, @ImapUid, @Subject, @FromAddress, @ToAddresses, @Body, @ReceivedDate, @IsHtml, @Folder, @Validity, @UniqueId)
            ON CONFLICT DO NOTHING
            """;

        var connectionString = dbContext.Database.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var batch = new NpgsqlBatch(connection);

        foreach (var email in emailList)
        {
            batch.BatchCommands.Add(new NpgsqlBatchCommand(sql)
            {
                Parameters =
                {
                    new() { ParameterName = "Id", Value = email.Id },
                    new() { ParameterName = "ImapUid", Value = email.ImapUid ?? (object)DBNull.Value },
                    new() { ParameterName = "Subject", Value = email.Subject },
                    new() { ParameterName = "FromAddress", Value = email.FromAddress },
                    new() { ParameterName = "ToAddresses", Value = email.ToAddresses },
                    new() { ParameterName = "Body", Value = email.Body },
                    new() { ParameterName = "ReceivedDate", Value = email.ReceivedDate },
                    new() { ParameterName = "IsHtml", Value = email.IsHtml },
                    new() { ParameterName = "Folder", Value = email.Folder ?? (object)DBNull.Value },
                    new() { ParameterName = "Validity", Value = email.UniqueId?.Validity ?? 0 },
                    new() { ParameterName = "UniqueId", Value = email.UniqueId?.Id ?? 0 }
                }
            });
        }

        await batch.ExecuteNonQueryAsync(ct);
        
        return emailList.Count;
    }

    public async Task<Dictionary<string, string>> GetExistingImapUidsByFolderAsync(CancellationToken ct = default)
    {
        var result = await dbContext.Emails
            .Where(e => e.ImapUid != null)
            .Select(e => new { e.ImapUid, e.Folder })
            .ToDictionaryAsync(e => e.ImapUid!, e => e.Folder!, ct);
        
        return result;
    }

    public async Task<Dictionary<string, string>> GetAllFolderIdByNameAsync(CancellationToken ct = default)
    {
        return await dbContext.MailFolders.ToDictionaryAsync(f => f.Name, f => f.Id, ct);
    }

    public async Task InsertFoldersAsync(IEnumerable<MailFolder> folders, CancellationToken ct = default)
    {
        dbContext.MailFolders.AddRange(folders);
        await dbContext.SaveChangesAsync(ct);
    }
}
