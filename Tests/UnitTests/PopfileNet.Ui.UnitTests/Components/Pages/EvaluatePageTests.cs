using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.UnitTests.TestHelpers;
using Bunit;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Components.Pages;

public sealed class EvaluatePageTests : BunitContext
{
    public EvaluatePageTests()
    {
        Services.AddSingleton(new LibraryConfiguration());
    }

    [Fact]
    public void EvaluatePage_WhenDevModeDisabled_DisplaysMessage()
    {
        var mockApi = new DevModeDisabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        
        var message = cut.Find("p");
        Assert.Contains("This page is only accessible in dev mode.", message.InnerHtml);
        Assert.DoesNotContain(cut.FindAll("div"), e => e.InnerHtml.Contains("Loading"));
    }

    [Fact]
    public void EvaluatePage_WhenDevModeEnabled_DisplaysEvaluationHeading()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        
        var heading = cut.Find("h2");
        Assert.Equal("Evaluation", heading.TextContent.Trim());
    }

    [Fact]
    public void EvaluatePage_WhenDevModeNotSet_DefaultsToFalse()
    {
        var mockApi = new DevModeNullMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        
        var message = cut.Find("p");
        Assert.Contains("This page is only accessible in dev mode.", message.InnerHtml);
    }

    [Fact]
    public void EvaluatePage_WithConfigLoaded_ShowsRunButton()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        
        var button = cut.FindAll("button, fluent-button").FirstOrDefault(e => e.InnerHtml.Contains("Run Evaluation"));
        Assert.NotNull(button);
        Assert.Contains("Folder Filter", cut.FindAll("td").First(e => e.InnerHtml.Contains("Folder Filter")).InnerHtml);
        Assert.Contains("Bucket Filter", cut.FindAll("td").First(e => e.InnerHtml.Contains("Bucket Filter")).InnerHtml);
    }

    [Fact]
    public void EvaluatePage_ShowsTrainTestSplitSlider()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        
        var label = cut.FindAll("td").First(e => e.InnerHtml.Contains("Train/Test Split"));
        Assert.NotNull(label);
    }

    [Fact]
    public async Task EvaluatePage_RunEvaluation_DisplaysResult()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        await cut.WaitForElementAsync("fluent-button");
        
        var button = cut.FindAll("button, fluent-button").FirstOrDefault(e => e.InnerHtml.Contains("Run Evaluation"));
        if (button == null) throw new Exception("Button not found");
        
        await button.ClickAsync();

        Assert.Contains("Overall Accuracy", cut.FindAll("h3").Select(h => h.TextContent));
        Assert.Contains("Correct", cut.FindAll("td").First(e => e.InnerHtml.Contains("Correct")).InnerHtml);
    }

    [Fact]
    public async Task EvaluatePage_ChangeCutoffToDate_ShowsDateInput()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        await cut.WaitForElementAsync("input[type='radio']");
        
        var radios = cut.FindAll("input[type='radio']");
        await radios[0].ClickAsync();

        await cut.WaitForStateAsync(() => cut.FindAll("input[type='date']").Any());

        Assert.NotEmpty(cut.FindAll("input[type='date']"));
        Assert.Empty(cut.FindAll("#cutoff-value-row input[type='number']"));
    }

    [Fact]
    public async Task EvaluatePage_ChangeCutoffToAmount_ShowsAmountInput()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        await cut.WaitForElementAsync("input[type='radio']");
        
        var radios = cut.FindAll("input[type='radio']");
        await radios[1].ClickAsync();

        await cut.WaitForStateAsync(() => cut.FindAll("input[type='number']").Any());

        Assert.NotEmpty(cut.FindAll("#cutoff-value-row input[type='number']"));
        Assert.Empty(cut.FindAll("#cutoff-value-row input[type='date']"));
    }

    [Fact]
    public async Task EvaluatePage_ApiError_DisplaysErrorMessage()
    {
        var mockApi = new ErrorMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        await cut.WaitForElementAsync("fluent-button");
        
        var button = cut.FindAll("button, fluent-button").FirstOrDefault(e => e.InnerHtml.Contains("Run Evaluation"));
        if (button == null) throw new Exception("Button not found");
        
        await button.ClickAsync();

        var errorDiv = cut.Find("div[style*='color:red']");
        Assert.Contains("Error: API failure", errorDiv.InnerHtml);
    }

    [Fact]
    public async Task EvaluatePage_MultipleRuns_DisplaysAggregatedResults()
    {
        var mockApi = new MultiRunMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();
        await cut.WaitForElementAsync("fluent-button");
        
        var button = cut.FindAll("button, fluent-button").FirstOrDefault(e => e.InnerHtml.Contains("Run Evaluation"));
        if (button == null) throw new Exception("Button not found");
        
        await button.ClickAsync();

        var heading = cut.FindAll("h3").First(e => e.InnerHtml.Contains("Aggregated Results"));
        Assert.NotNull(heading);
        Assert.Contains("Mean Accuracy", cut.FindAll("td").First(e => e.InnerHtml.Contains("Mean Accuracy")).InnerHtml);
    }


    private sealed class ErrorMock : DevModeEnabledMock
    {
        public override Task<EvaluationResult?> RunEvaluationAsync(EvaluationRequest request) =>
            Task.FromException<EvaluationResult?>(new Exception("API failure"));
    }

    private sealed class MultiRunMock : DevModeEnabledMock
    {
        public override Task<EvaluationResult?> RunEvaluationAsync(EvaluationRequest request) =>
            Task.FromResult<EvaluationResult?>(new EvaluationResult
            {
                NumberOfRuns = 3,
                Runs = [
                    new RunResultDto(1, 10, 5, 0.8f, 4, 5, [], []),
                    new RunResultDto(2, 10, 5, 0.7f, 3, 5, [], []),
                    new RunResultDto(3, 10, 5, 0.9f, 4, 5, [], [])
                ],
                Aggregated = new AggregatedMetricsDto(0.8f, 0.7f, 0.9f, [])
            });
    }

    private sealed class DevModeDisabledMock : MockApiClient
    {
        public override Task<bool?> GetDevModeStatusAsync() => Task.FromResult<bool?>(false);
    }

    private class DevModeEnabledMock : MockApiClient
    {
        private static readonly string[] MockFolders = ["Inbox", "Sent"];
        private static readonly object[] MockBuckets = [new { Id = "1", Name = "Work" }];

        public override Task<bool?> GetDevModeStatusAsync() => Task.FromResult<bool?>(true);
        public override Task<object?> GetEvaluationConfigAsync() =>
            Task.FromResult<object?>(new
            {
                Folders = MockFolders,
                Buckets = MockBuckets
            });
        public override Task<EvaluationResult?> RunEvaluationAsync(EvaluationRequest request) =>
            Task.FromResult<EvaluationResult?>(new EvaluationResult
            {
                NumberOfRuns = 1,
                Runs = [new RunResultDto(1, 10, 5, 0.8f, 4, 5, [], [])]
            });
    }

    private sealed class DevModeNullMock : MockApiClient
    {
        public override Task<bool?> GetDevModeStatusAsync() => Task.FromResult<bool?>(null);
    }
}
