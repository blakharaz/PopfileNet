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
        .WithDatabase($"popfilenet_{Guid.NewGuid():D}")  // Unique DB name per test run
        .WithUsername("test")
        .WithPassword("test")
        .Build();
    
    private string _postgresVolumeName = $"popfilenet_pgdata_{Guid.NewGuid():D}";
    
    private Process? _backendProcess;
    private Process? _uiProcess;
    public string UiUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Testcontainers handles cleanup automatically - just start fresh each time
        
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
                    ["ConnectionStrings__popfilenet"] = connectionString
                }
            };

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
                        Console.WriteLine($"Backend is ready at {backendUrl}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempts + 1}: {ex.Message}");
                }
                attempts++;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            if (attempts >= maxAttempts)
            {
                throw new InvalidOperationException($"Backend failed to start at {backendUrl} after {maxAttempts} seconds");
            }

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
            // Don't dispose the container here - let Testcontainers handle cleanup
            // The container will be disposed when the test run completes or on error
            if (backendStarted)
            {
                _backendProcess?.Kill(true);
                _backendProcess?.Dispose();
            }
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        // Testcontainers handles cleanup internally
        // Just ensure processes are killed
        _uiProcess?.Kill(true);
        _uiProcess?.Dispose();
        _backendProcess?.Kill(true);
        _backendProcess?.Dispose();
        
        // Dispose the PostgreSQL container to release resources
        await _postgres.DisposeAsync();
    }

    public async Task RestartBackendAsync()
    {
        // Kill the existing backend process if it's running
        _backendProcess?.Kill(true);
        _backendProcess?.Dispose();
        
        // Small delay to ensure process is fully terminated
        await Task.Delay(1000);
        
        // Start the backend process again
        var backendStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{SolutionRoot}/PopfileNet.Backend/PopfileNet.Backend.csproj\" --urls http://localhost:5180 --environment Test",
            WorkingDirectory = SolutionRoot,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
            EnvironmentVariables =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Test"
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
                    Console.WriteLine("Backend is ready after restart");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Backend restart attempt {i + 1}: {ex.Message}");
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new InvalidOperationException($"Backend failed to start after restart after {maxAttempts} seconds");
    }
}