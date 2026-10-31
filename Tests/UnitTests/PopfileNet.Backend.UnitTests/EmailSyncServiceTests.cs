using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database.Repositories;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public class EmailSyncServiceTests
{
    private readonly Mock<IEmailRepository> _emailRepository = new();
    private readonly Mock<IImapService> _imapService = new();

    private EmailSyncService CreateService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_emailRepository.Object);
        services.AddSingleton(_imapService.Object);
        var provider = services.BuildServiceProvider();
        return new EmailSyncService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            MockLogger());
    }

    private static ILogger<EmailSyncService> MockLogger() => NullLogger<EmailSyncService>.Instance;

    private void SetupImapConfigured(params FolderInfo[] folders)
    {
        _imapService
            .Setup(i => i.IsConfiguredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _imapService
            .Setup(i => i.GetAllPersonalFoldersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(folders.ToList());
        _emailRepository
            .Setup(r => r.GetAllFolderIdByNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(folders.ToDictionary(f => f.FullName, f => $"folder-id-{f.Name}"));
        _emailRepository
            .Setup(r => r.GetExistingImapUidsByFolderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _imapService
            .Setup(i => i.FetchEmailIdsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task TryRunSyncAsync_MarksFolderSyncing_AndCompletes_WithNoNewEmails()
    {
        SetupImapConfigured(new FolderInfo("f1", "INBOX", "INBOX"));

        var service = CreateService();
        var result = await service.TryRunSyncAsync();

        result.ShouldBeTrue();
        _emailRepository.Verify(
            r => r.MarkFolderSyncingAsync("folder-id-INBOX", It.IsAny<CancellationToken>()),
            Times.Once);
        _emailRepository.Verify(
            r => r.CompleteFolderSyncAsync("folder-id-INBOX", 0, It.IsAny<CancellationToken>()),
            Times.Once);
        _emailRepository.Verify(
            r => r.FailFolderSyncAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TryRunSyncAsync_SyncsNewEmails_AndRecordsCountPerFolder()
    {
        SetupImapConfigured(new FolderInfo("f1", "INBOX", "INBOX"));
        _imapService
            .Setup(i => i.FetchEmailIdsAsync("INBOX", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new EmailId(100, 5)]);
        _imapService
            .Setup(i => i.FetchEmailsAsync(It.IsAny<IEnumerable<EmailId>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Email { Id = "mail-1", ImapUid = "100:5", Subject = "Test" }]);
        _emailRepository
            .Setup(r => r.InsertEmailsIgnoringDuplicatesAsync(It.IsAny<IEnumerable<Email>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();
        var result = await service.TryRunSyncAsync();

        result.ShouldBeTrue();
        _emailRepository.Verify(
            r => r.CompleteFolderSyncAsync("folder-id-INBOX", 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryRunSyncAsync_UsesValidityIdKey_ToDetectExistingEmails()
    {
        SetupImapConfigured(new FolderInfo("f1", "INBOX", "INBOX"));
        _emailRepository
            .Setup(r => r.GetExistingImapUidsByFolderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { ["100:5"] = "folder-id-INBOX" });
        _imapService
            .Setup(i => i.FetchEmailIdsAsync("INBOX", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new EmailId(100, 5)]);

        var service = CreateService();
        await service.TryRunSyncAsync();

        _imapService.Verify(
            i => i.FetchEmailsAsync(It.IsAny<IEnumerable<EmailId>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailRepository.Verify(
            r => r.CompleteFolderSyncAsync("folder-id-INBOX", 0, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryRunSyncAsync_FailsFolder_WhenFetchingEmailIdsThrows()
    {
        SetupImapConfigured(new FolderInfo("f1", "INBOX", "INBOX"));
        _imapService
            .Setup(i => i.FetchEmailIdsAsync("INBOX", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("connection lost"));

        var service = CreateService();
        var result = await service.TryRunSyncAsync();

        result.ShouldBeTrue();
        _emailRepository.Verify(
            r => r.FailFolderSyncAsync("folder-id-INBOX", "connection lost", It.IsAny<CancellationToken>()),
            Times.Once);
        _emailRepository.Verify(
            r => r.CompleteFolderSyncAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TryRunSyncAsync_ContinuesWalk_WhenOneFolderFails()
    {
        SetupImapConfigured(
            new FolderInfo("f1", "INBOX", "INBOX"),
            new FolderInfo("f2", "Archive", "Archive"));
        _imapService
            .Setup(i => i.FetchEmailIdsAsync("INBOX", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("imap error"));
        _imapService
            .Setup(i => i.FetchEmailIdsAsync("Archive", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService();
        await service.TryRunSyncAsync();

        _emailRepository.Verify(
            r => r.FailFolderSyncAsync("folder-id-INBOX", "imap error", It.IsAny<CancellationToken>()),
            Times.Once);
        _emailRepository.Verify(
            r => r.CompleteFolderSyncAsync("folder-id-Archive", 0, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryRunSyncAsync_InsertsNewFolders_BeforeSyncing()
    {
        SetupImapConfigured(new FolderInfo("f1", "Sent", "Sent"));

        // First lookup happens before insertion (empty), second one after insertion.
        _emailRepository
            .SetupSequence(r => r.GetAllFolderIdByNameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([])
            .ReturnsAsync(new Dictionary<string, string> { ["Sent"] = "folder-id-Sent" });

        List<MailFolder>? insertedFolders = null;
        _emailRepository
            .Setup(r => r.InsertFoldersAsync(It.IsAny<IEnumerable<MailFolder>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<MailFolder>, CancellationToken>((folders, _) => insertedFolders = folders.ToList());

        var service = CreateService();
        await service.TryRunSyncAsync();

        insertedFolders.ShouldNotBeNull();
        insertedFolders.ShouldContain(f => f.Name == "Sent");
    }

    [Fact]
    public async Task TryRunSyncAsync_SkipsWalk_WhenImapNotConfigured()
    {
        _imapService
            .Setup(i => i.IsConfiguredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();
        var result = await service.TryRunSyncAsync();

        result.ShouldBeTrue();
        _imapService.Verify(
            i => i.GetAllPersonalFoldersAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        _emailRepository.Verify(
            r => r.MarkFolderSyncingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
