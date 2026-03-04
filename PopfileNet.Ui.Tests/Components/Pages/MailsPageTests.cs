using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.Tests.TestHelpers;
using Xunit;

namespace PopfileNet.Ui.Tests.Components.Pages;

public class MailsPageTests : BunitContext
{
    public MailsPageTests()
    {
        Services.AddSingleton(new LibraryConfiguration());
        Services.AddSingleton<IApiClient>(new MockApiClient());
    }
    
    [Fact]
    public void Mails_RendersPageTitle()
    {
        var cut = Render<Mails>();
        
        Assert.Contains("Emails", cut.Markup);
    }
    
    [Fact]
    public async Task Mails_DisplaysNoEmailsMessageWhenEmpty()
    {
        var cut = Render<Mails>();
        
        // Wait for async initialization
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("No emails in database.", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public void Mails_HasCorrectCard()
    {
        var cut = Render<Mails>();
        
        Assert.NotNull(cut.Find("[data-testid='mails-card']"));
    }
    
    [Fact]
    public void Mails_HasCorrectStack()
    {
        var cut = Render<Mails>();
        
        Assert.NotNull(cut.Find("[data-testid='mails-stack']"));
    }
}
