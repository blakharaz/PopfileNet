using System.Diagnostics;
using FluentAssertions;
using Microsoft.Playwright;
using Reqnroll;
using Testcontainers.PostgreSql;
using Xunit;

namespace PopfileNet.FunctionalTests;

public static class TestServices
{
    private static readonly Lazy<AppServices> _instance = new(() => new AppServices());
    public static AppServices Instance => _instance.Value;
}

public class AppServices : IAsyncLifetime
{
    private static string SolutionRoot
    {
        get
        {
            var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
            if (!string.IsNullOrEmpty(githubWorkspace) && Directory.Exists(githubWorkspace))
            {
                return githubWorkspace;
            }
            
            var path = AppContext.BaseDirectory;
            for (var i = 0; i < 6; i++)
            {
                var parent = Directory.GetParent(path);
                if (parent == null) break;
                path = parent.FullName;
            }
            return path;
        }
    }

    private readonly PostgreSqlContainer _postgres;
    private Process? _backendProcess;
    private Process? _uiProcess;
    public string UiUrl { get; private set; } = string.Empty;

    public AppServices()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("popfilenet")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var connectionString = _postgres.GetConnectionString();
        
        var backendUrl = "http://localhost:5180";
        UiUrl = "http://localhost:5181";

        Console.WriteLine($"Solution root: {SolutionRoot}");

        var backendStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{SolutionRoot}/PopfileNet.Backend/PopfileNet.Backend.csproj\" --urls {backendUrl} --environment Test",
            WorkingDirectory = SolutionRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            EnvironmentVariables =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Test",
                ["ConnectionStrings__DefaultConnection"] = connectionString,
                ["SKIP_DB_INIT"] = "true",
                ["ImapSettings__Server"] = "imap.test.com",
                ["ImapSettings__Port"] = "993",
                ["ImapSettings__Username"] = "test@test.com",
                ["ImapSettings__Password"] = "test",
                ["ImapSettings__UseSsl"] = "true"
            }
        };

        _backendProcess = Process.Start(backendStartInfo);
        if (_backendProcess == null)
        {
            throw new InvalidOperationException("Failed to start backend process");
        }

        var uiStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{SolutionRoot}/PopfileNet.Ui/PopfileNet.Ui.csproj\" --urls {UiUrl} --environment Test",
            WorkingDirectory = SolutionRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            EnvironmentVariables =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Test",
                ["services__popfilenet-backend__http__0"] = backendUrl,
                ["ConnectionStrings__DefaultConnection"] = connectionString,
                ["SKIP_DB_INIT"] = "true"
            }
        };

        _uiProcess = Process.Start(uiStartInfo);
        if (_uiProcess == null)
        {
            throw new InvalidOperationException("Failed to start UI process");
        }

        var maxAttempts = 60;
        for (var i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(UiUrl);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"UI is ready at {UiUrl}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {i + 1}: {ex.Message}");
            }
            await Task.Delay(1000);
        }
        
        throw new InvalidOperationException($"UI failed to start at {UiUrl} after {maxAttempts} seconds");
    }

    public async Task DisposeAsync()
    {
        _uiProcess?.Kill(true);
        _uiProcess?.Dispose();
        _backendProcess?.Kill(true);
        _backendProcess?.Dispose();
        await _postgres.DisposeAsync();
    }
}

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
    public void ThenThePageShouldLoadSuccessfully()
    {
        if (!_browserInstalled)
        {
            throw new SkipException("Playwright browsers not installed - run 'npx playwright install'");
        }

        var content = _page!.ContentAsync().Result;
        content.Should().NotBeNullOrEmpty();
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
