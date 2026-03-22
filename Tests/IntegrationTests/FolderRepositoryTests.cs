using Microsoft.EntityFrameworkCore;
using PopfileNet.Common;
using PopfileNet.Database.Repositories;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class FolderRepositoryTests(DatabaseFixture fixture)
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
    public async Task InsertFolders_SavesSuccessfully()
    {
        await ClearTablesAsync();
        
        await using var context = _fixture.CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        var repository = new EmailRepository(context);

        var folders = new List<MailFolder>
        {
            new() { Id = $"folder-1-{Guid.NewGuid():N}", Name = $"INBOX-{Guid.NewGuid():N}" },
            new() { Id = $"folder-2-{Guid.NewGuid():N}", Name = $"Sent-{Guid.NewGuid():N}" },
            new() { Id = $"folder-3-{Guid.NewGuid():N}", Name = $"Drafts-{Guid.NewGuid():N}" }
        };

        await repository.InsertFoldersAsync(folders);

        var count = await context.MailFolders.CountAsync();
        count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAllFolderIdByNameAsync_ReturnsCorrectMapping()
    {
        await ClearTablesAsync();
        
        await using var context = _fixture.CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        var repository = new EmailRepository(context);

        var inboxId = $"id-inbox-{Guid.NewGuid():N}";
        var sentId = $"id-sent-{Guid.NewGuid():N}";
        var folders = new List<MailFolder>
        {
            new() { Id = inboxId, Name = $"INBOX-{Guid.NewGuid():N}" },
            new() { Id = sentId, Name = $"Sent-{Guid.NewGuid():N}" }
        };

        await repository.InsertFoldersAsync(folders);

        var result = await repository.GetAllFolderIdByNameAsync();

        result.ShouldContainKey(folders[0].Name);
        result[folders[0].Name].ShouldBe(inboxId);
        result.ShouldContainKey(folders[1].Name);
        result[folders[1].Name].ShouldBe(sentId);
    }
}
