using System.Diagnostics;
using System.Net;
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
        .WithDatabase($"popfilenet_{Guid.NewGuid():D}")
        .WithUsername(Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "test")
        .WithPassword(Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "test")
        .Build();
    private Process? _backendProcess;
    private Process? _uiProcess;
    private readonly List<string> _backendErrors = [];
    private readonly List<string> _backendOutput = [];
    private readonly List<string> _uiErrors = [];
    private readonly List<string> _uiOutput = [];
    public string UiUrl { get; private set; } = string.Empty;
    public string BackendUrl { get; private set; } = string.Empty;
    public ApiHelper Api { get; private set; } = null!;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        var backendStarted = false;
        try
        {
            if (_initialized) return;

            await _postgres.StartAsync();

            var connectionString = _postgres.GetConnectionString();

            BackendUrl = "http://127.0.0.1:5180";
            UiUrl = "http://127.0.0.1:5181";
            var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
            Api = new ApiHelper(new HttpClient(handler) { BaseAddress = new Uri(BackendUrl) }, handler, connectionString);

            Console.WriteLine($"Solution root: {SolutionRoot}");

            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL")
                             ?? throw new InvalidOperationException("ADMIN_EMAIL environment variable is required");
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
                               ?? throw new InvalidOperationException("ADMIN_PASSWORD environment variable is required");
            var imapUsername = Environment.GetEnvironmentVariable("IMAP_USERNAME")
                               ?? throw new InvalidOperationException("IMAP_USERNAME environment variable is required");
            var imapPassword = Environment.GetEnvironmentVariable("IMAP_PASSWORD")
                               ?? throw new InvalidOperationException("IMAP_PASSWORD environment variable is required");

            var backendStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"exec \"{SolutionRoot}/PopfileNet.Backend/bin/Release/net10.0/PopfileNet.Backend.dll\" --environment Test \"--ConnectionStrings:popfilenet={connectionString}\" \"--ImapSettings:Server=localhost\" \"--ImapSettings:Port=993\" \"--ImapSettings:Username={imapUsername}\" \"--ImapSettings:Password={imapPassword}\"",
                WorkingDirectory = SolutionRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                EnvironmentVariables =
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Test",
                    ["ASPNETCORE_URLS"] = "http://127.0.0.1:5180;http://[::1]:5180",
                    ["AdminEmail"] = adminEmail,
                    ["AdminPassword"] = adminPassword,
                }
            };

            _backendProcess = Process.Start(backendStartInfo);
            if (_backendProcess == null)
            {
                throw new InvalidOperationException("Failed to start backend process");
            }
            backendStarted = true;

            _backendProcess.OutputDataReceived += (_, e) => { if (e.Data != null) _backendOutput.Add(e.Data); };
            _backendProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) _backendErrors.Add(e.Data); };
            _backendProcess.BeginOutputReadLine();
            _backendProcess.BeginErrorReadLine();

            Console.WriteLine($"Backend started, waiting for readiness...");

            const int maxAttempts = 120;
            var attempts = 0;
            while (attempts < maxAttempts)
            {
                if (_backendProcess.HasExited)
                {
                    var allErrors = string.Join("\n", _backendErrors);
                    var allOutput = string.Join("\n", _backendOutput);
                    throw new InvalidOperationException(
                        $"Backend process exited prematurely (exit code: {_backendProcess.ExitCode}).\n" +
                        $"Errors:\n{allErrors}\n" +
                        $"Output:\n{allOutput}");
                }

                try
                {
                    using var client = new HttpClient();
                    var response = await client.GetAsync(BackendUrl);
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine($"Backend is ready at {BackendUrl}");
                        await Api.LoginAsync(adminEmail, adminPassword);
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
                var allErrors = string.Join("\n", _backendErrors);
                var allOutput = string.Join("\n", _backendOutput);
                throw new InvalidOperationException(
                    $"Backend failed to start at {BackendUrl} after {maxAttempts} seconds.\n" +
                    $"Errors:\n{allErrors}\n" +
                    $"Output:\n{allOutput}");
            }

            var uiStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"exec \"{SolutionRoot}/PopfileNet.Ui/bin/Release/net10.0/PopfileNet.Ui.dll\" --environment Test \"--ConnectionStrings:popfilenet={connectionString}\"",
                WorkingDirectory = SolutionRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                EnvironmentVariables =
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Test",
                    ["ASPNETCORE_URLS"] = "http://127.0.0.1:5181;http://[::1]:5181",
                    ["services__popfilenet-backend__http__0"] = BackendUrl,
                }
            };

            _uiProcess = Process.Start(uiStartInfo);
            if (_uiProcess == null)
            {
                throw new InvalidOperationException("Failed to start UI process");
            }

            _uiProcess.OutputDataReceived += (_, e) => { if (e.Data != null) _uiOutput.Add(e.Data); };
            _uiProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) _uiErrors.Add(e.Data); };
            _uiProcess.BeginOutputReadLine();
            _uiProcess.BeginErrorReadLine();

            const int uiMaxAttempts = 120;
            for (var i = 0; i < uiMaxAttempts; i++)
            {
                try
                {
                    using var client = new HttpClient();
                    var response = await client.GetAsync(UiUrl);
                    if ((int)response.StatusCode != 0)
                    {
                        Console.WriteLine($"UI is ready at {UiUrl}");
                        _initialized = true;
                        return;
                    }
                }
                catch
                {
                    // Expected before UI startup
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            var allUiErrors = string.Join("\n", _uiErrors);
            var allUiOutput = string.Join("\n", _uiOutput);
            throw new InvalidOperationException(
                $"UI failed to start at {UiUrl} after {uiMaxAttempts} seconds.\n" +
                $"Errors:\n{allUiErrors}\n" +
                $"Output:\n{allUiOutput}");
        }
        catch
        {
            KillProcessSafely(_backendProcess, backendStarted);
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static void KillProcessSafely(Process? process, bool wasStarted = true)
    {
        if (process == null || !wasStarted) return;
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
        catch
        {
            // Ignore other kill errors
        }
        finally
        {
            try { process.Dispose(); } catch { }
        }
    }

    public async Task DisposeAsync()
    {
        KillProcessSafely(_uiProcess);
        KillProcessSafely(_backendProcess);
        await _postgres.DisposeAsync();
    }

    public async Task RestartBackendAsync()
    {
        var connectionString = _postgres.GetConnectionString();

        KillProcessSafely(_backendProcess);
        _backendProcess = null;
        _backendErrors.Clear();
        _backendOutput.Clear();

        await Task.Delay(1000);

        var imapUsername = Environment.GetEnvironmentVariable("IMAP_USERNAME")
                           ?? throw new InvalidOperationException("IMAP_USERNAME environment variable is required");
        var imapPassword = Environment.GetEnvironmentVariable("IMAP_PASSWORD")
                           ?? throw new InvalidOperationException("IMAP_PASSWORD environment variable is required");

        var backendStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"exec \"{SolutionRoot}/PopfileNet.Backend/bin/Release/net10.0/PopfileNet.Backend.dll\" --environment Test \"--ConnectionStrings:popfilenet={connectionString}\" \"--ImapSettings:Server=localhost\" \"--ImapSettings:Port=993\" \"--ImapSettings:Username={imapUsername}\" \"--ImapSettings:Password={imapPassword}\"",
            WorkingDirectory = SolutionRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            EnvironmentVariables =
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Test",
                ["ASPNETCORE_URLS"] = "http://127.0.0.1:5180;http://[::1]:5180",
                ["AdminEmail"] = Environment.GetEnvironmentVariable("ADMIN_EMAIL")
                                 ?? throw new InvalidOperationException("ADMIN_EMAIL environment variable is required"),
                ["AdminPassword"] = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
                                  ?? throw new InvalidOperationException("ADMIN_PASSWORD environment variable is required"),
            }
        };

        _backendProcess = Process.Start(backendStartInfo);
        if (_backendProcess == null)
        {
            throw new InvalidOperationException("Failed to restart backend process");
        }

        _backendProcess.OutputDataReceived += (_, e) => { if (e.Data != null) _backendOutput.Add(e.Data); };
        _backendProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) _backendErrors.Add(e.Data); };
        _backendProcess.BeginOutputReadLine();
        _backendProcess.BeginErrorReadLine();

        const int maxAttempts = 60;
        for (var i = 0; i < maxAttempts; i++)
        {
            if (_backendProcess.HasExited)
            {
                var allErrors = string.Join("\n", _backendErrors);
                throw new InvalidOperationException(
                    $"Backend process exited after restart (exit code: {_backendProcess.ExitCode}).\nErrors:\n{allErrors}");
            }

            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync("http://127.0.0.1:5180/health");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Backend is ready after restart");
                    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL")
                                     ?? throw new InvalidOperationException("ADMIN_EMAIL environment variable is required");
                    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
                                       ?? throw new InvalidOperationException("ADMIN_PASSWORD environment variable is required");
                    await Api.LoginAsync(adminEmail, adminPassword);
                    return;
                }
            }
            catch
            {
                // Expected before backend starts
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new InvalidOperationException($"Backend failed to start after restart after {maxAttempts} seconds");
    }
}
