using Shouldly;
using Microsoft.Extensions.Logging;
using Moq;
using PopfileNet.Backend.Services;
using PopfileNet.Backend.Models;
using PopfileNet.Common;
using PopfileNet.Imap;
using PopfileNet.Imap.Services;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace PopfileNet.Backend.UnitTests
{
    public class ImapServiceConfigurationTests
    {
        private readonly Mock<ISettingsService> _mockSettings;
        private readonly Mock<ILogger<ImapClientService>> _mockLogger;
        private readonly Mock<IImapClientFactory> _mockFactory;

        public ImapServiceConfigurationTests()
        {
            _mockSettings = new Mock<ISettingsService>();
            _mockLogger = new Mock<ILogger<ImapClientService>>();
            _mockFactory = new Mock<IImapClientFactory>();
        }

        private ImapService CreateService(AppSettings? settings = null)
        {
            _mockSettings.Setup(s => s.GetImapSettingsOnlyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings?.ImapSettings ?? new ImapSettingsDto());

            return new ImapService(_mockSettings.Object, _mockLogger.Object, _mockFactory.Object);
        }

        [Fact]
        public async Task IsConfiguredAsync_WithEmptySettings_ReturnsFalse()
        {
            var service = CreateService(new AppSettings());

            var configured = await service.IsConfiguredAsync();

            configured.ShouldBeFalse();
        }

        [Fact]
        public async Task TestConnectionAsync_Unconfigured_ReturnsFalse()
        {
            var service = CreateService(new AppSettings());

            var result = await service.TestConnectionAsync();

            result.ShouldBeFalse();
        }

        [Fact]
        public async Task Operations_Unconfigured_ReturnEmptyLists()
        {
            var service = CreateService(new AppSettings());

            (await service.GetAllPersonalFoldersAsync()).ShouldBeEmpty();
            (await service.FetchEmailIdsAsync()).ShouldBeEmpty();
            (await service.FetchEmailsAsync([])).ShouldBeEmpty();
        }
    }
}
