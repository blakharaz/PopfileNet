using Shouldly;
using Microsoft.Playwright;
using Reqnroll;
using Xunit;

namespace PopfileNet.FunctionalTests;

[Binding]
public class UiNavigationSteps
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private bool _browserInstalled = true;

    [Given("the UI is running")]
    public async Task GivenTheUiIsRunning()
    {
        await TestServices.Instance.InitializeAsync();
    }

    [When("I navigate to the home page")]
    public async Task WhenINavigateToTheHomePage()
    {
        if (!_browserInstalled || _browser == null)
        {
            throw new SkipException("Playwright browsers not installed - run 'npx playwright install'");
        }

        var uiUrl = TestServices.Instance.UiUrl;
        if (string.IsNullOrEmpty(uiUrl))
        {
            throw new InvalidOperationException("UI URL not set - services may have failed to start");
        }

        _page = await _browser.NewPageAsync();
        await _page.GotoAsync(uiUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    [Then("the page should load successfully")]
    public async Task ThenThePageShouldLoadSuccessfully()
    {
        if (!_browserInstalled)
        {
            throw new SkipException("Playwright browsers not installed - run 'npx playwright install'");
        }

        var content = await _page!.ContentAsync();
        content.ShouldNotBeNullOrEmpty();
    }

    [BeforeScenario]
    public async Task InitializeBrowser()
    {
        try
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }
        catch (PlaywrightException)
        {
            _browserInstalled = false;
        }
    }

    [AfterScenario]
    public async Task Cleanup()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
        }
        if (_browser != null)
        {
            await _browser.CloseAsync();
        }
        _playwright?.Dispose();
    }

    [AfterTestRun]
    public static async Task CleanupServices()
    {
        await TestServices.Instance.DisposeAsync();
    }
}
