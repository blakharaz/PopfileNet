using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace PopfileNet.IntegrationTests;

public class GreenMailFixture : IAsyncLifetime
{
    private readonly ILogger<GreenMailFixture> _logger;
    private const int ImapPort = 3143;
    private const int SmtpPort = 3025;

    private readonly IContainer _container;

    public GreenMailFixture()
    {
        _logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<GreenMailFixture>();
        
        _container = new ContainerBuilder(image: "greenmail/standalone:2.1.8")
            .WithPortBinding(ImapPort, ImapPort)
            .WithPortBinding(SmtpPort, true)
            .WithEnvironment("GREENMAIL_OPTS", "-Dgreenmail.setup.test.all -Dgreenmail.users=test:test123 -Dgreenmail.startup.timeout=10000 -Dgreenmail.hostname=0.0.0.0")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(".*Starting GreenMail API server at.*"))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(ImapPort))
            .Build();
    }

    public IContainer Container => _container;

    public int ImapPortValue => _container.GetMappedPublicPort(ImapPort);
    public string ImapHost => "localhost";

    public string ImapConnectionString => $"imap://localhost:{ImapPortValue}";
    public string SmtpConnectionString => $"smtp://localhost:{_container.GetMappedPublicPort(SmtpPort)}";

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Starting GreenMail container...");
        
        try
        {
            await _container.StartAsync();
            Thread.Sleep(2000); // Wait for GreenMail to fully initialize
            _logger.LogInformation("GreenMail is ready at localhost:{Port}", ImapPortValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start GreenMail container");
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("GreenMailTests")]
public class GreenMailTestsCollection : ICollectionFixture<GreenMailFixture>
{
}
