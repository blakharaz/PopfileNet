using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.UnitTests.TestHelpers;
using PopfileNet.Ui.UnitTests.Utils;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Components.Pages;

public class SettingsPageTests : BunitContext
{
    public SettingsPageTests()
    {
        JSInterop.SetupFluentUiModules();
        
        Services.AddSingleton(new LibraryConfiguration());
        Services.AddSingleton<IApiClient>(new MockApiClient());
    }
    
    [Fact]
    public async Task Settings_RendersPageTitle()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsImapServerField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Server", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsPortField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Port", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsUsernameField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Username", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsPasswordField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Password", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsSaveButton()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Save", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsTestConnectionButton()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Test Connection", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsBucketsSection()
    {
        var cut = Render<Settings>();
        
        // Wait for async initialization
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Buckets", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsFolderMappingsSection()
    {
        var cut = Render<Settings>();
        
        // Wait for async initialization
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Folder Mappings", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public void Settings_ContainsAddBucketButton()
    {
        var cut = Render<Settings>();
        
        Assert.Contains("Add Bucket", cut.Markup);
    }
    
    [Fact]
    public async Task Settings_ContainsUseSslSwitch()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Use SSL", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsMaxParallelConnectionsField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Max Parallel Connections", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public void Settings_HasCorrectImapCard()
    {
        var cut = Render<Settings>();
        
        Assert.NotNull(cut.Find("[data-testid='settings-imap-card']"));
    }
    
    [Fact]
    public void Settings_HasCorrectBucketsCard()
    {
        var cut = Render<Settings>();
        
        Assert.NotNull(cut.Find("[data-testid='settings-buckets-card']"));
    }
}
