using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace PopfileNet.FunctionalTests;

public class AppServices : IAsyncLifetime
{
    private static readonly Lazy<string?> _lazyConnectionString = new(
        () => Environment.GetEnvironmentVariable("ConnectionStrings__popfilenet"));

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

    private readonly ILogger<AppServices> _logger =
        LoggerFactory.Create(b => b.AddConsole()).CreateLogger<AppServices>();

    // Only used when no external Postgres is available (e.g. local dev without a service)
    private PostgreSqlContainer? _postgres;
    
    private Process? _backendProcess;
    private Process? _uiProcess;
    public string UiUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var connectionString = await ResolvePostgresConnectionAsync();

        var backendStarted = false;
        try
        {
            const string backendUrl = "http://localhost:5180";
            UiUrl = "http://localhost:5181";

            Console.WriteLine($"Solution root: {SolutionRoot}");
            var source = _lazyConnectionString.Value != null ? "env" : "testcontainer";
            Console.WriteLine($"Using connection string from: {source}");

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
                    ["ConnectionStrings__popfilenet"] = connectionString
                }
            };

            // Start the backend process first so we can wait for it
            _backendProcess = Process.Start(backendStartInfo);
            if (_backendProcess == null)
            {
                throw new InvalidOperationException("Failed to start backend process");
            }
            backendStarted = true;

            _logger.LogInformation("Backend started, waiting for readiness...");

            // Wait for backend to be ready
            const int maxAttempts = 60;
            var attempts = 0;
            while (attempts < maxAttempts)
            {
                try
                {
                    using var client = new HttpClient();
                    var response = await client.GetAsync(backendUrl);
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInformation("Backend is ready at {Url}", backendUrl);
                        }

                        break;
                    }
                }
                catch
                {
                    // Connection refused or other error — expected before startup
                }
                attempts++;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            if (attempts >= maxAttempts)
            {
                _logger.LogError("Backend failed to start after waiting");
                throw new InvalidOperationException($"Backend failed to start at {backendUrl} after {maxAttempts} seconds");
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
                    ["ConnectionStrings__popfilenet"] = connectionString,
                    ["services__popfilenet-backend__http__0"] = backendUrl
                }
            };

            _uiProcess = Process.Start(uiStartInfo);
            if (_uiProcess == null)
            {
                throw new InvalidOperationException("Failed to start UI process");
            }

            const int uiMaxAttempts = 60;
            for (var i = 0; i < uiMaxAttempts; i++)
            {
                try
                {
                    using var client = new HttpClient();
                    var response = await client.GetAsync(UiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("UI is ready at {UiUrl}", UiUrl);
                        return;
                    }
                }
                catch
                {
                    // Expected before UI startup
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            throw new InvalidOperationException($"UI failed to start at {UiUrl} after {maxAttempts} seconds");
        }
        catch
        {
            if (backendStarted && _backendProcess != null)
            {
                try
                {
                    await DrainOutputAsync(_backendProcess);
                }
                catch
                {
                    // Ignore drain errors during cleanup
                }

                _backendProcess.Kill(true);
                _backendProcess.Dispose();
            }
            throw;
        }
    }

    private async Task<string> ResolvePostgresConnectionAsync()
    {
        // Use env-provided connection string if available (e.g., from GitHub service container)
        var connectionString = _lazyConnectionString.Value;
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Fall back to Testcontainers for local dev / CI without a dedicated Postgres service
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Starting PostgreSQL container...");
        }

        _postgres = new PostgreSqlBuilder(image: "postgres:16-alpine")
            .WithDatabase($"popfilenet_{Guid.NewGuid():D}")  // Unique DB name per test run
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgres.StartAsync();
        return _postgres.GetConnectionString();
    }

    private static async Task DrainOutputAsync(Process process)
    {
        var t1 = process.StandardOutput.ReadToEndAsync();
        await Task.WhenAll(t1, process.StandardError.ReadToEndAsync());
    }

    public async Task DisposeAsync()
    {
        _uiProcess?.Kill(true);
        _uiProcess?.Dispose();
        _backendProcess?.Kill(true);
        _backendProcess?.Dispose();
        
        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
        }
    }

    public async Task RestartBackendAsync()
    {
        // Kill the existing backend process if it's running
        var connectionString = await ResolvePostgresConnectionAsync();
        
        _backendProcess?.Kill(true);
        await DrainOutputAsync(_backendProcess!).ConfigureAwait(false);
        _backendProcess?.Dispose();
        
        // Small delay to ensure process is fully terminated
        await Task.Delay(1000);

        var backendStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{SolutionRoot}/PopfileNet.Backend/PopfileNet.Backend.csproj\" --urls http://localhost:5180 --environment Test",
            WorkingDirectory = SolutionRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            EnvironmentVariables =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Test",
                ["ConnectionStrings__popfilenet"] = connectionString
            }
        };

        _backendProcess = Process.Start(backendStartInfo);
        if (_backendProcess == null)
        {
            throw new InvalidOperationException("Failed to restart backend process");
        }

        // Wait for backend to be ready
        const int maxAttempts = 30;
        for (var i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync("http://localhost:5180/health");
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Backend is ready after restart");
                    return;
                }
            }
            catch
            {
                // Expected before backend starts
            }
        }

        throw new InvalidOperationException($"Backend failed to start after restart after {maxAttempts} seconds");
    }
}
