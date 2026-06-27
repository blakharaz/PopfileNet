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

        Assert.Contains("This page is only accessible in dev mode.", cut.Markup);
        Assert.DoesNotContain("Loading", cut.Markup);
    }

    [Fact]
    public void EvaluatePage_WhenDevModeEnabled_DisplaysEvaluationHeading()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();

        Assert.Contains("Evaluation", cut.Markup);
    }

    [Fact]
    public void EvaluatePage_WhenDevModeNotSet_DefaultsToFalse()
    {
        var mockApi = new DevModeNullMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();

        Assert.Contains("This page is only accessible in dev mode.", cut.Markup);
    }

    [Fact]
    public void EvaluatePage_WithConfigLoaded_ShowsRunButton()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();

        Assert.Contains("Run Evaluation", cut.Markup);
        Assert.Contains("Folder Filter", cut.Markup);
        Assert.Contains("Bucket Filter", cut.Markup);
    }

    [Fact]
    public void EvaluatePage_ShowsTrainTestSplitSlider()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();

        Assert.Contains("Train/Test Split", cut.Markup);
    }

    [Fact]
    public void EvaluatePage_ShowsNumberOfRunsInput()
    {
        var mockApi = new DevModeEnabledMock();
        Services.AddSingleton<IApiClient>(mockApi);

        var cut = Render<Evaluate>();

        Assert.Contains("Number of Runs", cut.Markup);
    }

    private sealed class DevModeDisabledMock : MockApiClient
    {
        public override Task<bool?> GetDevModeStatusAsync() => Task.FromResult<bool?>(false);
    }

    private sealed class DevModeEnabledMock : MockApiClient
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
