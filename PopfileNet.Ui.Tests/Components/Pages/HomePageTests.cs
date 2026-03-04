using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using Xunit;

namespace PopfileNet.Ui.Tests.Components.Pages;

public class HomePageTests : BunitContext
{
    public HomePageTests()
    {
        Services.AddSingleton(new LibraryConfiguration());
    }
    
    [Fact]
    public void HomePage_RendersCorrectly()
    {
        var cut = Render<Home>();
        
        Assert.Contains("PopfileNet", cut.Markup);
        Assert.Contains("Getting Started", cut.Markup);
    }
    
    [Fact]
    public void HomePage_ContainsNavigationLinks()
    {
        var cut = Render<Home>();
        
        Assert.Contains("Settings", cut.Markup);
        Assert.Contains("Mail Sync", cut.Markup);
        Assert.Contains("Classifier", cut.Markup);
    }
}
