using Microsoft.EntityFrameworkCore;
using PopfileNet.Common;
using PopfileNet.Database.Repositories;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class FolderSyncStatusTests(DatabaseFixture fixture)
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

    private async Task<(EmailRepository Repository, string FolderId)> CreateFolderWithRepositoryAsync()
    {
        var context = _fixture.CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        var repository = new EmailRepository(context);
        var folder = new MailFolder { Id = $"folder-{Guid.NewGuid():N}", Name = $"INBOX-{Guid.NewGuid():N}" };
        await repository.InsertFoldersAsync([folder]);

        return (repository, folder.Id);
    }

    [Fact]
    public async Task MarkFolderSyncingAsync_SetsStatusToSyncing()
    {
        await ClearTablesAsync();

        var (repository, folderId) = await CreateFolderWithRepositoryAsync();
        await using var context = _fixture.CreateDbContext();

        await repository.MarkFolderSyncingAsync(folderId);

        var folder = await context.MailFolders.SingleAsync(f => f.Id == folderId);
        folder.SyncStatus.ShouldBe(FolderSyncStatus.Syncing);
    }

    [Fact]
    public async Task CompleteFolderSyncAsync_SetsIdleWithTimestampAndCount()
    {
        await ClearTablesAsync();

        var (repository, folderId) = await CreateFolderWithRepositoryAsync();
        await using var context = _fixture.CreateDbContext();

        await repository.MarkFolderSyncingAsync(folderId);
        await repository.CompleteFolderSyncAsync(folderId, 7);

        var folder = await context.MailFolders.SingleAsync(f => f.Id == folderId);
        folder.SyncStatus.ShouldBe(FolderSyncStatus.Idle);
        folder.LastSyncAt.ShouldNotBeNull();
        folder.LastSyncAt.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));
        folder.LastSyncEmailCount.ShouldBe(7);
        folder.LastSyncError.ShouldBeNull();
    }

    [Fact]
    public async Task FailFolderSyncAsync_SetsErrorWithMessage()
    {
        await ClearTablesAsync();

        var (repository, folderId) = await CreateFolderWithRepositoryAsync();
        await using var context = _fixture.CreateDbContext();

        await repository.FailFolderSyncAsync(folderId, "imap error");

        var folder = await context.MailFolders.SingleAsync(f => f.Id == folderId);
        folder.SyncStatus.ShouldBe(FolderSyncStatus.Error);
        folder.LastSyncError.ShouldBe("imap error");
        folder.LastSyncAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task FailFolderSyncAsync_TruncatesLongErrorMessages()
    {
        await ClearTablesAsync();

        var (repository, folderId) = await CreateFolderWithRepositoryAsync();
        await using var context = _fixture.CreateDbContext();

        var longError = new string('x', 1500);
        await repository.FailFolderSyncAsync(folderId, longError);

        var folder = await context.MailFolders.SingleAsync(f => f.Id == folderId);
        folder.LastSyncError!.Length.ShouldBe(1000);
    }

    [Fact]
    public async Task ResetStaleSyncingFoldersAsync_ResetStuckFolders_ReturnsCountOfResetFolders()
    {
        await ClearTablesAsync();

        var context = _fixture.CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        var repository = new EmailRepository(context);
        var stuckFolder = new MailFolder { Id = $"folder-stuck-{Guid.NewGuid():N}", Name = $"Stuck-{Guid.NewGuid():N}", SyncStatus = FolderSyncStatus.Syncing };
        var idleFolder = new MailFolder { Id = $"folder-idle-{Guid.NewGuid():N}", Name = $"Idle-{Guid.NewGuid():N}" };
        await repository.InsertFoldersAsync([stuckFolder, idleFolder]);

        var resetCount = await repository.ResetStaleSyncingFoldersAsync();

        resetCount.ShouldBe(1);
        await using var verifyContext = _fixture.CreateDbContext();
        var stuck = await verifyContext.MailFolders.SingleAsync(f => f.Id == stuckFolder.Id);
        stuck.SyncStatus.ShouldBe(FolderSyncStatus.Error);
        stuck.LastSyncError.ShouldNotBeNullOrEmpty();
        var idle = await verifyContext.MailFolders.SingleAsync(f => f.Id == idleFolder.Id);
        idle.SyncStatus.ShouldBe(FolderSyncStatus.Idle);
    }
}
