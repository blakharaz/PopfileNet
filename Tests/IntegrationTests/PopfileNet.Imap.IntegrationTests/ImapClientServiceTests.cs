using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;
using Shouldly;
using Xunit;

namespace PopfileNet.Imap.IntegrationTests;

[Collection("GreenMailTests")]
public class ImapClientServiceTests(GreenMailFixture fixture)
{
    private readonly GreenMailFixture _fixture = fixture;

    private ImapClientService CreateService()
    {
        var settings = new ImapSettings
        {
            Server = "localhost",
            Port = 3143,
            Username = "test",
            Password = "test123",
            UseSsl = false,
            MaxParallelConnections = 2
        };
        var logger = new LoggerFactory().CreateLogger<ImapClientService>();
        var factory = new ImapClientFactory();
        return new ImapClientService(Options.Create(settings), logger, factory);
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidCredentials_ReturnsTrue()
    {
        var service = CreateService();

        var result = await service.TestConnectionAsync();

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_WithInvalidCredentials_ReturnsFalse()
    {
        var settings = new ImapSettings
        {
            Server = "localhost",
            Port = 3143,
            Username = "test",
            Password = "wrongpassword",
            UseSsl = false
        };
        var logger = new LoggerFactory().CreateLogger<ImapClientService>();
        var factory = new ImapClientFactory();
        var service = new ImapClientService(Options.Create(settings), logger, factory);

        var result = await service.TestConnectionAsync();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAllPersonalFoldersAsync_WithDefaultSetup_ReturnsInbox()
    {
        var service = CreateService();

        var result = await service.GetAllPersonalFoldersAsync();

        result.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task FetchEmailIdsAsync_WithValidFolder_ReturnsEmailIds()
    {
        var service = CreateService();

        var result = await service.FetchEmailIdsAsync("INBOX");

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task FetchEmailsAsync_WithEmptyList_ReturnsEmptyList()
    {
        var service = CreateService();

        var result = await service.FetchEmailsAsync([], "INBOX");

        result.ShouldBeEmpty();
    }
}
