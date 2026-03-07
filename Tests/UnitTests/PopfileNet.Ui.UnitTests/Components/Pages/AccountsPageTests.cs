using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.UnitTests.TestHelpers;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Components.Pages;

public class AccountsPageTests : BunitContext
{
    public AccountsPageTests()
    {
        Services.AddSingleton(new LibraryConfiguration());
        Services.AddSingleton<IApiClient>(new MockApiClient());
    }
    
    [Fact]
    public void Accounts_RendersPageTitle()
    {
        var cut = Render<Accounts>();
        
        Assert.Contains("Mail Accounts", cut.Markup);
    }
    
    [Fact]
    public async Task Accounts_DisplaysNoAccountsMessageWhenEmpty()
    {
        var cut = Render<Accounts>();
        
        // Wait for async initialization
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("No accounts configured.", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public void Accounts_HasCorrectCard()
    {
        var cut = Render<Accounts>();
        
        Assert.NotNull(cut.Find("[data-testid='accounts-card']"));
    }
    
    [Fact]
    public void Accounts_HasCorrectStack()
    {
        var cut = Render<Accounts>();
        
        Assert.NotNull(cut.Find("[data-testid='accounts-stack']"));
    }
}

