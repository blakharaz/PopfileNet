using FluentAssertions;
using MailKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PopfileNet.Common;
using PopfileNet.Imap;
using PopfileNet.Imap.Models;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;
using Xunit;
using IMailFolder = MailKit.IMailFolder;

namespace PopfileNet.Imap.Tests;

public class ImapClientServiceTests
{
    private readonly Mock<IImapClientFactory> _mockFactory;
    private readonly Mock<IImapClient> _mockClient;
    private readonly Mock<ILogger<ImapClientService>> _mockLogger;
    private readonly ImapSettings _settings;

    public ImapClientServiceTests()
    {
        _mockFactory = new Mock<IImapClientFactory>();
        _mockClient = new Mock<IImapClient>();
        _mockLogger = new Mock<ILogger<ImapClientService>>();
        _settings = new ImapSettings
        {
            Server = "imap.example.com",
            Port = 993,
            Username = "test",
            Password = "password",
            UseSsl = true,
            MaxParallelConnections = 2
        };
    }

    [Fact]
    public async Task TestConnectionAsync_ConnectSuccess_ReturnsTrue()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        
        var service = CreateService();

        var result = await service.TestConnectionAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_ConnectFailure_ReturnsFalse()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));
        
        var service = CreateService();

        var result = await service.TestConnectionAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task FetchEmailIdsAsync_ValidFolder_ReturnsEmailIds()
    {
        var mockFolder = new Mock<IMailFolder>();
        var mockInbox = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.Inbox).Returns(mockInbox.Object);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        
        var uniqueIds = new[] { new UniqueId(1, 1), new UniqueId(1, 2) };
        mockFolder.Setup(f => f.SearchAsync(It.IsAny<MailKit.Search.SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uniqueIds);
        
        var service = CreateService();

        var result = await service.FetchEmailIdsAsync("Inbox");

        result.Should().HaveCount(2);
        result[0].Validity.Should().Be(1u);
        result[0].Id.Should().Be(1u);
    }

    [Fact]
    public async Task FetchEmailIdsAsync_EmptyFolder_ReturnsEmptyList()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        
        mockFolder.Setup(f => f.SearchAsync(It.IsAny<MailKit.Search.SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UniqueId>());
        
        var service = CreateService();

        var result = await service.FetchEmailIdsAsync("Inbox");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchEmailIdsAsync_NullFolder_UsesInbox()
    {
        var mockInbox = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.Inbox).Returns(mockInbox.Object);
        
        mockInbox.Setup(f => f.SearchAsync(It.IsAny<MailKit.Search.SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new UniqueId(1, 1) });
        
        var service = CreateService();

        var result = await service.FetchEmailIdsAsync(null);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task FetchEmailsAsync_ValidIds_ReturnsEmails()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        _mockClient.Setup(c => c.Inbox).Returns(mockFolder.Object);
        
        var emailIds = new List<EmailId> { new(Validity: 1, Id: 1) };
        
        var mimeMessage = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        mockFolder.Setup(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Returns(mimeMessage);
        
        var service = CreateService();

        var result = await service.FetchEmailsAsync(emailIds, "INBOX");

        result.Should().HaveCount(1);
        result[0].Subject.Should().Be("Test Subject");
    }

    [Fact]
    public async Task FetchEmailsAsync_EmptyIds_ReturnsEmptyList()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        
        var service = CreateService();

        var result = await service.FetchEmailsAsync(Array.Empty<EmailId>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllPersonalFoldersAsync_ReturnsFolders()
    {
        var mockNamespace = new FolderNamespace('/', "INBOX");
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.PersonalNamespaces).Returns(new[] { mockNamespace });
        _mockClient.Setup(c => c.GetFoldersAsync(mockNamespace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IMailFolder> { mockFolder.Object });
        
        var service = CreateService();

        var result = await service.GetAllPersonalFoldersAsync();

        result.Should().HaveCount(1);
    }

    private ImapClientService CreateService()
    {
        return new ImapClientService(
            Options.Create(_settings),
            _mockLogger.Object,
            _mockFactory.Object);
    }

    private static MimeKit.MimeMessage CreateMimeMessage(string subject, string body, string from)
    {
        var message = new MimeKit.MimeMessage();
        message.Subject = subject;
        message.Body = new MimeKit.TextPart("plain") { Text = body };
        message.From.Add(MimeKit.MailboxAddress.Parse(from));
        return message;
    }
}
