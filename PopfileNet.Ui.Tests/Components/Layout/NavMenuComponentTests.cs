using Bunit;
using PopfileNet.Ui.Components.Layout;
using Xunit;

namespace PopfileNet.Ui.Tests.Components.Layout;

public class NavMenuComponentTests : BunitContext
{
    [Fact]
    public void NavMenu_RendersNavigation()
    {
        var cut = Render<NavMenu>();
        
        Assert.Contains("PopfileNet.Ui", cut.Markup);
    }
    
    [Fact]
    public void NavMenu_ContainsHomeLink()
    {
        var cut = Render<NavMenu>();
        
        Assert.Contains("Home", cut.Markup);
    }
    
    [Fact]
    public void NavMenu_ContainsAccountsLink()
    {
        var cut = Render<NavMenu>();
        
        Assert.Contains("Accounts", cut.Markup);
    }
    
    [Fact]
    public void NavMenu_ContainsMailsLink()
    {
        var cut = Render<NavMenu>();
        
        Assert.Contains("Mails", cut.Markup);
    }
    
    [Fact]
    public void NavMenu_ContainsSyncLink()
    {
        var cut = Render<NavMenu>();
        
        Assert.Contains("Sync", cut.Markup);
    }
    
    [Fact]
    public void NavMenu_ContainsClassifyLink()
    {
        var cut = Render<NavMenu>();
        
        Assert.Contains("Classify", cut.Markup);
    }
    
    [Fact]
    public void NavMenu_ContainsSettingsLink()
    {
        var cut = Render<NavMenu>();
        
        Assert.Contains("Settings", cut.Markup);
    }
    
    [Fact]
    public void NavMenu_NavLinksHaveCorrectHref()
    {
        var cut = Render<NavMenu>();
        
        var markup = cut.Markup;
        Assert.Contains("href=\"accounts\"", markup);
        Assert.Contains("href=\"mails\"", markup);
        Assert.Contains("href=\"sync\"", markup);
        Assert.Contains("href=\"classify\"", markup);
        Assert.Contains("href=\"settings\"", markup);
    }
    
    [Fact]
    public void NavMenu_HasCorrectNavbar()
    {
        var cut = Render<NavMenu>();
        
        Assert.NotNull(cut.Find("[data-testid='navbar']"));
    }
    
    [Fact]
    public void NavMenu_HasCorrectNavbarBrand()
    {
        var cut = Render<NavMenu>();
        
        Assert.NotNull(cut.Find("[data-testid='navbar-brand']"));
    }
}
