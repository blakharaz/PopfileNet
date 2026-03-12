using System.Diagnostics;
using Testcontainers.PostgreSql;
using Xunit;

namespace PopfileNet.FunctionalTests;

public class AppServices : IAsyncLifetime
{
    private static string SolutionRoot
    {
        get
        {
            var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
            var startPath = !string.IsNullOrEmpty(githubWorkspace) && Directory.Exists(githubWorkspace)
                ? githubWorkspace
                : AppContext.BaseDirectory;

            return FindRoot(startPath);
        }
    }

    private static string FindRoot(string startPath)
    {
        var path = startPath;
        while (path != null)
        {
            if (Directory.GetFiles(path, "*.sln").Length > 0)
            {
                return path;
            }

            var parent = Directory.GetParent(path);
            if (parent == null)
            {
                break;
            }
            path = parent.FullName;
        }

        throw new InvalidOperationException($"Could not find solution root starting from {startPath}");
    }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(image: "postgres:16-alpine")
        .WithDatabase("popfilenet")
        .WithUsername("test")
        .WithPassword("test")
        .Build();
    private Process? _backendProcess;
    private Process? _uiProcess;
    public string UiUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var postgresStarted = false;
        var backendStarted = false;

        try
        {
            await _postgres.StartAsync();
            postgresStarted = true;

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
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
                EnvironmentVariables =
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Test",
                    ["ConnectionStrings__popfilenet"] = connectionString,
                    ["SkipDbInit"] = "true"
                }
            };

            _backendProcess = Process.Start(backendStartInfo);
            if (_backendProcess == null)
            {
                throw new InvalidOperationException("Failed to start backend process");
            }
            backendStarted = true;

            var uiStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{SolutionRoot}/PopfileNet.Ui/PopfileNet.Ui.csproj\" --urls {UiUrl} --environment Test",
                WorkingDirectory = SolutionRoot,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
                EnvironmentVariables =
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Test",
                    ["ConnectionStrings__popfilenet"] = connectionString,
                    ["SkipDbInit"] = "true",
                    ["services__popfilenet-backend__http__0"] = backendUrl
                }
            };

            _uiProcess = Process.Start(uiStartInfo);
            if (_uiProcess == null)
            {
                throw new InvalidOperationException("Failed to start UI process");
            }

            const int maxAttempts = 60;
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
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            throw new InvalidOperationException($"UI failed to start at {UiUrl} after {maxAttempts} seconds");
        }
        catch
        {
            if (backendStarted)
            {
                _backendProcess?.Kill(true);
                _backendProcess?.Dispose();
            }
            if (postgresStarted)
            {
                await _postgres.DisposeAsync();
            }
            throw;
        }
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