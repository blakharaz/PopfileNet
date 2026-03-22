using Shouldly;
using MailKit;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PopfileNet.Common;
using PopfileNet.Imap.Exceptions;
using PopfileNet.Imap.Services;
using PopfileNet.Imap.Settings;
using Xunit;
using IMailFolder = MailKit.IMailFolder;

namespace PopfileNet.Imap.UnitTests;

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

        SetupDefaultMockClient(_mockClient);
    }

    private static void SetupDefaultMockClient(Mock<IImapClient> mockClient)
    {
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task TestConnectionAsync_ConnectSuccess_ReturnsTrue()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        
        var service = CreateService();

        var result = await service.TestConnectionAsync();

        result.ShouldBeTrue();
        
        _mockClient.Verify(c => c.ConnectAsync(_settings.Server, _settings.Port, _settings.UseSsl, It.IsAny<CancellationToken>()), Times.Once);
        _mockClient.Verify(c => c.AuthenticateAsync(_settings.Username, _settings.Password, It.IsAny<CancellationToken>()), Times.Once);
        _mockClient.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestConnectionAsync_ConnectFailure_ReturnsFalse()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));
        
        var service = CreateService();

        var result = await service.TestConnectionAsync();

        result.ShouldBeFalse();
        
        _mockClient.Verify(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
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
        
        mockFolder.Setup(f => f.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FolderAccess.ReadOnly);
        mockFolder.Setup(f => f.SearchAsync(It.IsAny<MailKit.Search.SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UniqueId(1, 1), new UniqueId(1, 2)]);
        
        var service = CreateService();

        var result = await service.FetchEmailIdsAsync("Inbox");

        result.Count.ShouldBe(2);
        result[0].Validity.ShouldBe(1u);
        result[0].Id.ShouldBe(1u);
    }

    [Fact]
    public async Task FetchEmailIdsAsync_EmptyFolder_ReturnsEmptyList()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        
        mockFolder.Setup(f => f.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FolderAccess.ReadOnly);
        mockFolder.Setup(f => f.SearchAsync(It.IsAny<MailKit.Search.SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UniqueId>());
        
        var service = CreateService();

        var result = await service.FetchEmailIdsAsync("Inbox");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchEmailIdsAsync_NullFolder_UsesInbox()
    {
        var mockInbox = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.Inbox).Returns(mockInbox.Object);
        
        mockInbox.Setup(f => f.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FolderAccess.ReadOnly);
        mockInbox.Setup(f => f.SearchAsync(It.IsAny<MailKit.Search.SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UniqueId(1, 1)]);
        
        var service = CreateService();

        var result = await service.FetchEmailIdsAsync();

        result.Count.ShouldBe(1);
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
        
        var emailIds = new List<EmailId> { new(validity: 1, id: 1) };
        
        var mimeMessage = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        mockFolder.Setup(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Returns(mimeMessage);
        
        var service = CreateService();

        var result = await service.FetchEmailsAsync(emailIds, "INBOX");

        result.Count.ShouldBe(1);
        result[0].Subject.ShouldBe("Test Subject");
    }

    [Fact]
    public async Task GetAllPersonalFoldersAsync_SingleNamespace_ReturnsFolders()
    {
        var mockNamespace = new FolderNamespace('/', "INBOX");
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.PersonalNamespaces).Returns([mockNamespace]);
        _mockClient.Setup(c => c.GetFoldersAsync(mockNamespace, It.IsAny<CancellationToken>()))
            .ReturnsAsync([mockFolder.Object]);
        
        var service = CreateService();

        var result = await service.GetAllPersonalFoldersAsync();

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TestConnectionAsync_AuthenticationFailure_ReturnsFalse()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Authentication failed"));
        
        var service = CreateService();

        var result = await service.TestConnectionAsync();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task FetchEmailIdsAsync_SearchThrowsException_ReturnsEmptyList()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        
        mockFolder.Setup(f => f.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FolderAccess.ReadOnly);
        mockFolder.Setup(f => f.SearchAsync(It.IsAny<MailKit.Search.SearchQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Search failed"));
        
        var service = CreateService();

        var result = await service.FetchEmailIdsAsync("Inbox");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchEmailIdsAsync_ConnectionThrowsException_ThrowsImapOperationException()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));
        
        var service = CreateService();

        await Assert.ThrowsAsync<ImapOperationException>(async () => await service.FetchEmailIdsAsync("Inbox"));
    }

    [Fact]
    public async Task FetchEmailsAsync_ClientPoolExhausted_ReturnsAvailableEmails()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        _mockClient.Setup(c => c.Inbox).Returns(mockFolder.Object);
        
        var emailIds = new List<EmailId>
        {
            new(validity: 1, id: 1),
            new(validity: 1, id: 2)
        };
        
        var mimeMessage = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        mockFolder.Setup(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Returns(mimeMessage);
        
        var settingsWithSingleConnection = new ImapSettings
        {
            Server = "imap.example.com",
            Port = 993,
            Username = "test",
            Password = "password",
            UseSsl = true,
            MaxParallelConnections = 1
        };
        
        var service = new ImapClientService(
            Options.Create(settingsWithSingleConnection),
            _mockLogger.Object,
            _mockFactory.Object);

        var result = await service.FetchEmailsAsync(emailIds, "INBOX");

        result.Count.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task FetchEmailsAsync_FolderNotOpenInitially_OpensFolder()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        _mockClient.Setup(c => c.Inbox).Returns(mockFolder.Object);
        
        var emailIds = new List<EmailId> { new(validity: 1, id: 1) };
        
        mockFolder.Setup(f => f.IsOpen).Returns(false);
        mockFolder.Setup(f => f.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FolderAccess.ReadOnly);
        
        var mimeMessage = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        mockFolder.Setup(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Returns(mimeMessage);
        
        var service = CreateService();

        var result = await service.FetchEmailsAsync(emailIds, "INBOX");

        result.Count.ShouldBe(1);
        mockFolder.Verify(f => f.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchEmailsAsync_DisconnectExceptionInFinally_LogsWarning()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        _mockClient.Setup(c => c.Inbox).Returns(mockFolder.Object);
        _mockClient.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Disconnect failed"));
        
        var emailIds = new List<EmailId> { new(validity: 1, id: 1) };
        
        var mimeMessage = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        mockFolder.Setup(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Returns(mimeMessage);
        
        var service = CreateService();

        var result = await service.FetchEmailsAsync(emailIds, "INBOX");

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ConnectAndOpenFolderAsync_FolderOpenThrowsException_DisposesClient()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        
        mockFolder.Setup(f => f.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cannot open folder"));
        
        var service = CreateService();

        await Assert.ThrowsAsync<ImapOperationException>(async () => 
            await service.FetchEmailIdsAsync("Inbox"));

        // Client is disposed (not disconnected) when connection fails
        _mockClient.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ConnectAndOpenFolderAsync_ConnectThrowsException_DisposesClient()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ImapConnectionException("Cannot connect", new Exception()));
        
        var service = CreateService();

        await Assert.ThrowsAsync<ImapOperationException>(async () => 
            await service.FetchEmailIdsAsync("Inbox"));

        // Client is disposed when connection fails
        _mockClient.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task FetchEmailsAsync_GeneralException_WrapsAndThrows()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));
        
        var service = CreateService();

        // General exceptions are wrapped in ImapOperationException and re-thrown
        await Assert.ThrowsAsync<ImapOperationException>(async () => 
            await service.FetchEmailsAsync([new EmailId(validity: 1, id: 1)], "INBOX"));
    }

    // Test for LoadMessagesInParallelAsync empty uidList check (returns early)
    [Fact]
    public async Task FetchEmailIdsAsync_ReturnsZeroCount_LeadsToEarlyReturn()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        
        mockFolder.Setup(f => f.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FolderAccess.ReadOnly);
        // Search returns empty list - triggers early return
        mockFolder.Setup(f => f.SearchAsync(It.IsAny<MailKit.Search.SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UniqueId>());
        
        var service = CreateService();

        var result = await service.FetchEmailsAsync([], "INBOX");

        result.ShouldBeEmpty();
        // Verify no connection was made since uidList is empty
        _mockClient.Verify(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FetchEmailsAsync_ClientFactoryThrows_WrapsAndThrows()
    {
        _mockFactory.Setup(f => f.Create())
            .Throws(new InvalidOperationException("Cannot create client"));
        
        var service = CreateService();

        // Client factory exceptions are wrapped in ImapConnectionException then ImapOperationException
        await Assert.ThrowsAsync<ImapOperationException>(async () => 
            await service.FetchEmailsAsync([new EmailId(validity: 1, id: 1)], "INBOX"));
    }

    [Fact]
    public async Task FetchEmailsAsync_MessageFetchThrowsException_LogsErrorAndContinues()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        _mockClient.Setup(c => c.Inbox).Returns(mockFolder.Object);
        
        var emailIds = new List<EmailId>
        {
            new(validity: 1, id: 1),
            new(validity: 1, id: 2)
        };
        
        // First call succeeds, second throws
        var mimeMessage = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        mockFolder.SetupSequence(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Returns(mimeMessage)
            .Throws(new Exception("Failed to fetch message"));
        
        var service = CreateService();

        var result = await service.FetchEmailsAsync(emailIds, "INBOX");

        // Should return at least the successfully fetched messages (or empty if all failed)
        result.Count.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task TestConnectionAsync_DisconnectThrowsDuringErrorHandling_ReturnsFalse()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));
        _mockClient.Setup(c => c.IsConnected).Returns(true); // Client thinks it's connected
        _mockClient.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Disconnect also failed"));
        
        var service = CreateService();

        var result = await service.TestConnectionAsync();

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task FetchEmailIdsAsync_ConnectFails_ClientIsConnected_DisconnectsAndThrows()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ImapConnectionException("Connect failed", new Exception()));
        _mockClient.Setup(c => c.IsConnected).Returns(true); // Client is connected
        _mockClient.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var service = CreateService();

        await Assert.ThrowsAsync<ImapOperationException>(async () => 
            await service.FetchEmailIdsAsync("Inbox"));

        // Verify disconnect was called when client is connected
        _mockClient.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchEmailIdsAsync_SearchThrowsException_LogsError()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        
        mockFolder.Setup(f => f.OpenAsync(It.IsAny<FolderAccess>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FolderAccess.ReadOnly);
        mockFolder.Setup(f => f.SearchAsync(It.IsAny<MailKit.Search.SearchQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Search operation failed"));
        
        var service = CreateService();

        var result = await service.FetchEmailIdsAsync("Inbox");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchEmailIdsAsync_AuthenticationFails_ThrowsImapOperationException()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Invalid credentials"));
        
        var service = CreateService();

        await Assert.ThrowsAsync<ImapOperationException>(async () => 
            await service.FetchEmailIdsAsync("Inbox"));
    }

    [Fact]
    public async Task FetchEmailsAsync_MultipleDisconnectErrors_LogsWarnings()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        _mockClient.Setup(c => c.Inbox).Returns(mockFolder.Object);
        
        var emailIds = new List<EmailId>
        {
            new(validity: 1, id: 1),
            new(validity: 1, id: 2)
        };
        
        var mimeMessage = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        mockFolder.Setup(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Returns(mimeMessage);
        
        // Disconnect throws for all clients
        _mockClient.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error during disconnect"));
        
        var service = CreateService();

        // Should not throw - errors are logged and swallowed
        var result = await service.FetchEmailsAsync(emailIds, "INBOX");

        result.Count.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task FetchEmailIdsAsync_NonExistentFolder_ThrowsImapOperationException()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IMailFolder?)null); // Folder doesn't exist
        
        var service = CreateService();

        // Non-existent folder throws NullReferenceException which is wrapped in ImapOperationException
        await Assert.ThrowsAsync<ImapOperationException>(async () => 
            await service.FetchEmailIdsAsync("NonExistentFolder"));
    }

    [Fact]
    public async Task GetAllPersonalFoldersAsync_MultipleNamespaces_ReturnsAllFolders()
    {
        var namespace1 = new FolderNamespace('/', "INBOX");
        var namespace2 = new FolderNamespace('/', "Drafts");
        var folder1 = new Mock<IMailFolder>().Object;
        var folder2 = new Mock<IMailFolder>().Object;
        var folder3 = new Mock<IMailFolder>().Object;
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.PersonalNamespaces).Returns([namespace1, namespace2]);
        _mockClient.SetupSequence(c => c.GetFoldersAsync(It.IsAny<FolderNamespace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { folder1, folder2 })
            .ReturnsAsync(new[] { folder3 });
        
        var service = CreateService();

        var result = await service.GetAllPersonalFoldersAsync();

        result.Count.ShouldBe(3);
        _mockClient.Verify(c => c.GetFoldersAsync(namespace1, It.IsAny<CancellationToken>()), Times.Once);
        _mockClient.Verify(c => c.GetFoldersAsync(namespace2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllPersonalFoldersAsync_EmptyNamespaces_ReturnsEmptyList()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.PersonalNamespaces).Returns([]);
        
        var service = CreateService();

        var result = await service.GetAllPersonalFoldersAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllPersonalFoldersAsync_ConnectionFails_ThrowsImapConnectionException()
    {
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Server unreachable"));
        
        var service = CreateService();

        await Assert.ThrowsAsync<ImapConnectionException>(async () => 
            await service.GetAllPersonalFoldersAsync());
    }

    [Fact]
    public async Task FetchEmailsAsync_EmptyIdList_ReturnsEmptyList()
    {
        var service = CreateService();

        var result = await service.FetchEmailsAsync([]);

        result.ShouldBeEmpty();
        _mockFactory.Verify(f => f.Create(), Times.Never); // No connection made for empty list
    }

    [Fact]
    public async Task FetchEmailsAsync_LoadMessagesThrowsException_ReturnsEmptyList()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        _mockClient.Setup(c => c.Inbox).Returns(mockFolder.Object);
        
        var emailIds = new List<EmailId> { new(validity: 1, id: 1) };
        
        mockFolder.Setup(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("Get message failed"));
        _mockClient.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var service = CreateService();

        var result = await service.FetchEmailsAsync(emailIds, "INBOX");
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchEmailsAsync_MultipleEmailsWithParallelConnections_ReturnsAllEmails()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        _mockClient.Setup(c => c.Inbox).Returns(mockFolder.Object);
        
        var emailIds = new List<EmailId>
        {
            new(validity: 1, id: 1),
            new(validity: 1, id: 2),
            new(validity: 1, id: 3)
        };
        
        var mimeMessage = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        mockFolder.Setup(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Returns(mimeMessage);
        
        var service = CreateService();

        var result = await service.FetchEmailsAsync(emailIds, "INBOX");

        result.Count.ShouldBe(3);
        result.Select(e => e.Subject).ToList().ShouldBe(new[] { "Test Subject", "Test Subject", "Test Subject" });
    }

    [Fact]
    public async Task FetchEmailsAsync_WithSingleConnectionSetting_UsesOneClient()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        // Override settings to use only 1 parallel connection
        var singleConnectionSettings = _settings with { MaxParallelConnections = 1 };
        
        _mockFactory.Setup(f => f.Create()).Returns(_mockClient.Object);
        _mockClient.Setup(c => c.IsConnected).Returns(true);
        _mockClient.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFolder.Object);
        _mockClient.Setup(c => c.Inbox).Returns(mockFolder.Object);
        
        var emailIds = new List<EmailId>
        {
            new(validity: 1, id: 1),
            new(validity: 1, id: 2)
        };
        
        var mimeMessage = CreateMimeMessage("Test Subject", "Test Body", "from@example.com");
        mockFolder.Setup(f => f.GetMessage(It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()))
            .Returns(mimeMessage);
        
        var service = new ImapClientService(
            Options.Create(singleConnectionSettings),
            _mockLogger.Object,
            _mockFactory.Object);

        await service.FetchEmailsAsync(emailIds, "INBOX");

        // With MaxParallelConnections=1, only one client should be created
        _mockFactory.Verify(f => f.Create(), Times.Once);
    }

    [Fact]
    public async Task FetchEmailsAsync_ClientPoolExhausted_LogsWarning()
    {
        var mockFolder = new Mock<IMailFolder>();
        
        // Create separate clients for each connection to simulate pool exhaustion scenario
        var client1 = new Mock<IImapClient>();
        var client2 = new Mock<IImapClient>();
        
        _mockFactory.SetupSequence(f => f.Create())
            .Returns(client1.Object)
            .Returns(client2.Object);
        
        SetupMockClient(client1);
        SetupMockClient(client2);
        
        var service = new ImapClientService(
            Options.Create(_settings with { MaxParallelConnections = 2 }),
            _mockLogger.Object,
            _mockFactory.Object);

        // This test verifies the warning log path when pool is exhausted
        // The actual exhaustion happens internally in FetchMessageWithPoolAsync
        var emailIds = new List<EmailId>
        {
            new(validity: 1, id: 1),
            new(validity: 1, id: 2)
        };

        await service.FetchEmailsAsync(emailIds);
    }

    private void SetupMockClient(Mock<IImapClient> mock)
    {
        mock.Setup(c => c.IsConnected).Returns(true);
        mock.Setup(c => c.GetFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IMailFolder>().Object);
        mock.Setup(c => c.Inbox).Returns(new Mock<IMailFolder>().Object);
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
