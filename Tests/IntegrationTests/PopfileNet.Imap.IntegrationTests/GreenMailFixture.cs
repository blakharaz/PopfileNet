using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace PopfileNet.Imap.IntegrationTests;

public class GreenMailFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("greenmail/standalone:2.0.0")
        .WithExposedPort(3143)
        .WithExposedPort(3025)
        .WithEnvironment("GREENMAIL_OPTS", "-Dgreenmail.setup.test=all -Dgreenmail.users=test:test123")
        .Build();

    public IContainer Container => _container;

    public string ImapConnectionString => $"imap://{_container.Hostname}:{_container.GetMappedPublicPort(3143)}";
    public string SmtpConnectionString => $"smtp://{_container.Hostname}:{_container.GetMappedPublicPort(3025)}";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await Task.Delay(5000);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("GreenMailTests")]
public class GreenMailTestsCollection : ICollectionFixture<GreenMailFixture>
{
}
