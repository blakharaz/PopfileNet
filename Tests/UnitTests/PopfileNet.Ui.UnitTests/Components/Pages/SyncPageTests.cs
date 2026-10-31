using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.UnitTests.TestHelpers;
using Shouldly;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Components.Pages;

public class SyncPageTests : BunitContext
{
    public SyncPageTests()
    {
        Services.AddSingleton(new LibraryConfiguration());
        Services.AddSingleton<IApiClient>(new MockApiClient());
    }
    
    [Fact]
    public void Sync_RendersPageTitle()
    {
        var cut = Render<Sync>();
        
        Assert.Contains("Mail Sync", cut.Markup);
    }
    
    [Fact]
    public void Sync_ContainsFolderRefreshButton()
    {
        var cut = Render<Sync>();
        
        Assert.Contains("Refresh Folders", cut.Markup);
    }
    
    [Fact]
    public void Sync_ContainsSyncAllButton()
    {
        var cut = Render<Sync>();
        
        Assert.Contains("Sync All Emails", cut.Markup);
    }
    
    [Fact]
    public void Sync_DisplaysDatabaseStatistics()
    {
        var cut = Render<Sync>();
        
        var markup = cut.Markup;
        Assert.Contains("Database Statistics", markup);
        Assert.Contains("Total Folders", markup);
        Assert.Contains("Total Emails", markup);
    }
    
    [Fact]
    public async Task Sync_DisplaysRecentEmailsSection()
    {
        var cut = Render<Sync>();
        
        // Wait for async initialization
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Recent Emails", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public void Sync_HasCorrectCard()
    {
        var cut = Render<Sync>();
        
        Assert.NotNull(cut.Find("[data-testid='sync-card']"));
    }
    
    [Fact]
    public void Sync_HasCorrectStack()
    {
        var cut = Render<Sync>();
        
        Assert.NotNull(cut.Find("[data-testid='sync-stack']"));
    }

    private class FolderMockApiClient(List<FolderDto> folders) : MockApiClient
    {
        public List<FolderDto> Folders { get; } = folders;

        public override Task<PagedResponse<FolderDto>?> GetFoldersAsync(int page = 1, int pageSize = 20) =>
            Task.FromResult<PagedResponse<FolderDto>?>(new PagedResponse<FolderDto>
            {
                Items = Folders,
                Page = 1,
                PageSize = 20,
                TotalCount = Folders.Count,
                TotalPages = 1,
                HasPrevious = false,
                HasNext = false,
                IsSuccess = true
            });

        public override Task<SyncJobResult?> SyncEmailsAsync() =>
            Task.FromResult<SyncJobResult?>(new SyncJobResult(true, "Sync started", 0));
    }

    [Fact]
    public async Task Sync_DisplaysStatusPerFolder()
    {
        Services.AddSingleton<IApiClient>(new FolderMockApiClient(
        [
            new("id-1", "INBOX", "Idle", DateTimeOffset.UtcNow.AddMinutes(-30), 5, null),
            new("id-2", "Archive", "Error", DateTimeOffset.UtcNow.AddMinutes(-20), 0, "imap error")
        ]));

        var cut = Render<Sync>();

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () =>
            {
                var statuses = cut.FindAll("[data-testid='folder-status']").Select(b => b.TextContent).ToList();
                statuses.ShouldContain("Idle");
                statuses.ShouldContain("Error");
            }, TimeSpan.FromSeconds(2)));

        Assert.NotNull(cut.Find("[data-testid='folder-error']"));
        Assert.Contains("imap error", cut.Markup);
    }

    [Fact]
    public async Task Sync_DisplaysLastSyncTimestampAndCountPerFolder()
    {
        var lastSync = DateTimeOffset.UtcNow.AddHours(-1);
        Services.AddSingleton<IApiClient>(new FolderMockApiClient(
        [
            new("id-1", "INBOX", "Idle", lastSync, 12, null)
        ]));

        var cut = Render<Sync>();

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("12", cut.Markup), TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task Sync_DisplaysSummaryWithErrors()
    {
        Services.AddSingleton<IApiClient>(new FolderMockApiClient(
        [
            new("id-1", "INBOX", "Idle", DateTimeOffset.UtcNow.AddMinutes(-30), 5, null),
            new("id-2", "Archive", "Error", DateTimeOffset.UtcNow.AddMinutes(-20), 0, "imap error")
        ]));

        var cut = Render<Sync>();

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.NotNull(cut.Find("[data-testid='sync-summary']")), TimeSpan.FromSeconds(2)));

        Assert.NotNull(cut.Find("[data-testid='sync-error-badge']"));
        Assert.Contains("1 folder(s) with errors", cut.Markup);
    }

    [Fact]
    public async Task Sync_DoesNotShowSummary_WhenNoFolderWasSynced()
    {
        Services.AddSingleton<IApiClient>(new FolderMockApiClient(
        [
            new("id-1", "INBOX", "Idle", null, 0, null)
        ]));

        var cut = Render<Sync>();

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.NotNull(cut.Find("[data-testid='folder-row-INBOX']")), TimeSpan.FromSeconds(2)));

        var summaryElementCount = cut.FindAll("[data-testid='sync-summary']").Count;
        summaryElementCount.ShouldBe(0);
    }
}
