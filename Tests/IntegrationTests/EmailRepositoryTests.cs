using Microsoft.EntityFrameworkCore;
using PopfileNet.Common;
using PopfileNet.Database.Repositories;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class EmailRepositoryTests(DatabaseFixture fixture)
{
    private readonly DatabaseFixture _fixture = fixture;

    private async Task ClearTablesAsync()
    {
        await using var context = _fixture.CreateDbContext();
        try
        {
#pragma warning disable EF1002 // Hardcoded table names, safe in test context
            var tables = new[] { "\"Emails\"", "\"MailFolders\"", "\"Buckets\"", "\"EmailHeaders\"" };
            foreach (var table in tables)
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {table} CASCADE");
                }
                catch
                {
                }
            }
#pragma warning restore EF1002
        }
        catch
        {
        }
    }

    [Fact]
    public async Task InsertEmailsIgnoringDuplicates_EmptyList_ReturnsZero()
    {
        await using var context = _fixture.CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        var repository = new EmailRepository(context);

        var result = await repository.InsertEmailsIgnoringDuplicatesAsync(new List<Email>());

        result.ShouldBe(0);
    }

    [Fact]
    public async Task GetExistingImapUidsByFolderAsync_WithEmails_ReturnsCorrectMapping()
    {
        await ClearTablesAsync();
        
        await using var context = _fixture.CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        var folder = new MailFolder { Id = $"folder-{Guid.NewGuid():N}", Name = $"INBOX-{Guid.NewGuid():N}" };
        context.MailFolders.Add(folder);
        await context.SaveChangesAsync();

        var emails = new List<Email>
        {
            new() { Id = $"email-1-{Guid.NewGuid():N}", Subject = "Test 1", FromAddress = "from1@test.com", ReceivedDate = DateTime.UtcNow, Folder = folder.Id, ImapUid = "1" },
            new() { Id = $"email-2-{Guid.NewGuid():N}", Subject = "Test 2", FromAddress = "from2@test.com", ReceivedDate = DateTime.UtcNow, Folder = folder.Id, ImapUid = "2" }
        };
        context.Emails.AddRange(emails);
        await context.SaveChangesAsync();

        var repository = new EmailRepository(context);

        var result = await repository.GetExistingImapUidsByFolderAsync();

        result.ShouldContainKey("1");
        result["1"].ShouldBe(folder.Id);
        result.ShouldContainKey("2");
        result["2"].ShouldBe(folder.Id);
    }
}
