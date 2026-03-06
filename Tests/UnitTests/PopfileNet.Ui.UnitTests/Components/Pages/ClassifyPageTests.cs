using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.Tests.TestHelpers;
using Xunit;

namespace PopfileNet.Ui.Tests.Components.Pages;

public class ClassifyPageTests : BunitContext
{
    public ClassifyPageTests()
    {
        Services.AddSingleton(new LibraryConfiguration());
        Services.AddSingleton<IApiClient>(new MockApiClient());
    }
    
    [Fact]
    public void Classify_RendersPageTitle()
    {
        var cut = Render<Classify>();
        
        Assert.Contains("Email Classifier", cut.Markup);
    }
    
    [Fact]
    public void Classify_ContainsTrainingSection()
    {
        var cut = Render<Classify>();
        
        Assert.Contains("Training", cut.Markup);
    }
    
    [Fact]
    public void Classify_ContainsPredictionSection()
    {
        var cut = Render<Classify>();
        
        Assert.Contains("Prediction", cut.Markup);
    }
    
    [Fact]
    public void Classify_ContainsCategoriesSection()
    {
        var cut = Render<Classify>();
        
        Assert.Contains("Categories", cut.Markup);
    }
    
    [Fact]
    public void Classify_ContainsTrainButton()
    {
        var cut = Render<Classify>();
        
        Assert.Contains("Train Model", cut.Markup);
    }
    
    [Fact]
    public async Task Classify_ContainsClassifyButton()
    {
        var cut = Render<Classify>();
        
        // Wait for async initialization
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Classify Selected Email", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Classify_DisplaysClassifierStatus()
    {
        var cut = Render<Classify>();
        
        // Wait for async initialization
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => 
            {
                var markup = cut.Markup;
                Assert.Contains("Status", markup);
                Assert.Contains("Training Data", markup);
            },
            TimeSpan.FromSeconds(2)));
    }
    

}
