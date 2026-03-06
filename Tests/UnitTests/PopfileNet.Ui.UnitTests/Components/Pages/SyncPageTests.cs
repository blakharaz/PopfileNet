using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.Tests.TestHelpers;
using Xunit;

namespace PopfileNet.Ui.Tests.Components.Pages;

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
}
