using Shouldly;
using Microsoft.EntityFrameworkCore;
using Moq;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Database;
using PopfileNet.Common;
using PopfileNet.Imap.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PopfileNet.Backend.UnitTests
{
    public class SettingsServiceTests
    {
        private readonly Mock<PopfileNetDbContext> _mockDb;
        private readonly Mock<ImapSettings> _mockDefaults;

        public SettingsServiceTests()
        {
            _mockDb = new Mock<PopfileNetDbContext>();
            _mockDefaults = new Mock<ImapSettings>();
            
            // Setup default values
            _mockDefaults.SetupGet(d => d.Server).Returns("default.server.com");
            _mockDefaults.SetupGet(d => d.Port).Returns(993);
            _mockDefaults.SetupGet(d => d.Username).Returns("default.user");
            _mockDefaults.SetupGet(d => d.Password).Returns("default.password");
            _mockDefaults.SetupGet(d => d.UseSsl).Returns(true);
            _mockDefaults.SetupGet(d => d.MaxParallelConnections).Returns(4);
        }

        private SettingsService CreateService()
        {
            return new SettingsService(_mockDb.Object, _mockDefaults.Object);
        }

        [Fact]
        public async Task GetSettingsAsync_ReturnsSettingsWithDefaults_WhenNoDbRecordExists()
        {
            // Arrange
            _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Settings?)null);
            _mockDb.Setup(db => db.Buckets.ToListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Bucket>());
            _mockDb.Setup(db => db.MailFolders.ToListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MailFolder>());
            
            var service = CreateService();

            // Act
            var result = await service.GetSettingsAsync();

            // Assert
            result.ImapSettings.ShouldNotBeNull();
            result.ImapSettings.Server.ShouldBe("default.server.com");
            result.ImapSettings.Port.ShouldBe(993);
            result.ImapSettings.Username.ShouldBe("default.user");
            result.ImapSettings.Password.ShouldBe(""); // Password is not returned for security
            result.ImapSettings.UseSsl.ShouldBeTrue();
            result.ImapSettings.MaxParallelConnections.ShouldBe(4);
            
            result.Buckets.ShouldBeEmpty();
            result.FolderMappings.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetSettingsAsync_ReturnsSettingsFromDb_WhenRecordExists()
        {
            // Arrange
            var dbSettings = new Settings
            {
                Id = 1,
                ImapServer = "db.server.com",
                ImapPort = 143,
                ImapUsername = "db.user",
                ImapPassword = "db.password",
                ImapUseSsl = false,
                MaxParallelConnections = 2
            };
            
            var dbBuckets = new List<Bucket>
            {
                new Bucket { Id = "bucket1", Name = "Work", Description = "Work emails" },
                new Bucket { Id = "bucket2", Name = "Personal", Description = "Personal emails" }
            };
            
            var dbFolders = new List<MailFolder>
            {
                new MailFolder { Id = "folder1", Name = "Inbox", BucketId = "bucket1" },
                new MailFolder { Id = "folder2", Name = "Archive", BucketId = "bucket2" }
            };
            
            _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dbSettings);
            _mockDb.Setup(db => db.Buckets.ToListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(dbBuckets);
            _mockDb.Setup(db => db.MailFolders.ToListAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(dbFolders);
            
            var service = CreateService();

            // Act
            var result = await service.GetSettingsAsync();

            // Assert
            result.ImapSettings.ShouldNotBeNull();
            result.ImapSettings.Server.ShouldBe("db.server.com");
            result.ImapSettings.Port.ShouldBe(143);
            result.ImapSettings.Username.ShouldBe("db.user");
            result.ImapSettings.Password.ShouldBe(""); // Password is not returned for security
            result.ImapSettings.UseSsl.ShouldBeFalse();
            result.ImapSettings.MaxParallelConnections.ShouldBe(2);
            
            result.Buckets.Count.ShouldBe(2);
            result.Buckets[0].Id.ShouldBe("bucket1");
            result.Buckets[0].Name.ShouldBe("Work");
            result.Buckets[0].Description.ShouldBe("Work emails");
            result.Buckets[1].Id.ShouldBe("bucket2");
            result.Buckets[1].Name.ShouldBe("Personal");
            result.Buckets[1].Description.ShouldBe("Personal emails");
            
            result.FolderMappings.Count.ShouldBe(2);
            result.FolderMappings[0].Name.ShouldBe("Inbox");
            result.FolderMappings[0].BucketId.ShouldBe("bucket1");
            result.FolderMappings[1].Name.ShouldBe("Archive");
            result.FolderMappings[1].BucketId.ShouldBe("bucket2");
        }

        [Fact]
        public async Task GetImapSettingsOnlyAsync_ReturnsImapSettingsWithDefaults_WhenNoDbRecordExists()
        {
            // Arrange
            _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Settings?)null);
            
            var service = CreateService();

            // Act
            var result = await service.GetImapSettingsOnlyAsync();

            // Assert
            result.Server.ShouldBe("default.server.com");
            result.Port.ShouldBe(993);
            result.Username.ShouldBe("default.user");
            result.Password.ShouldBe("default.password"); // Password IS returned here (needed for connection)
            result.UseSsl.ShouldBeTrue();
            result.MaxParallelConnections.ShouldBe(4);
        }

        [Fact]
        public async Task GetImapSettingsOnlyAsync_ReturnsImapSettingsFromDb_WhenRecordExists()
        {
            // Arrange
            var dbSettings = new Settings
            {
                Id = 1,
                ImapServer = "imap.db.com",
                ImapPort = 587,
                ImapUsername = "db.imap.user",
                ImapPassword = "db.imap.password",
                ImapUseSsl = true,
                MaxParallelConnections = 3
            };
            
            _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dbSettings);
            
            var service = CreateService();

            // Act
            var result = await service.GetImapSettingsOnlyAsync();

            // Assert
            result.Server.ShouldBe("imap.db.com");
            result.Port.ShouldBe(587);
            result.Username.ShouldBe("db.imap.user");
            result.Password.ShouldBe("db.imap.password");
            result.UseSsl.ShouldBeTrue();
            result.MaxParallelConnections.ShouldBe(3);
        }

        [Fact]
        public async Task SaveSettingsAsync_CreatesNewSettingsRecord_WhenNoRecordExists()
        {
            // Arrange
            _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Settings?)null);
            
            var settingsToSave = new AppSettings
            {
                ImapSettings = new ImapSettingsDto("new.server.com", 587, "new.user", "new.password", false, 5),
                Buckets = new List<BucketDto>(),
                FolderMappings = new List<FolderMappingDto>()
            };
            
            Settings? capturedSettings = null;
            _mockDb.Setup(db => db.Settings.Add(It.IsAny<Settings>()))
                .Callback<Settings>(s => capturedSettings = s);
            _mockDb.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            
            var service = CreateService();

            // Act
            await service.SaveSettingsAsync(settingsToSave);

            // Assert
            capturedSettings.ShouldNotBeNull();
            capturedSettings!.ImapServer.ShouldBe("new.server.com");
            capturedSettings.ImapPort.ShouldBe(587);
            capturedSettings.ImapUsername.ShouldBe("new.user");
            capturedSettings.ImapPassword.ShouldBe("new.password");
            capturedSettings.ImapUseSsl.ShouldBeFalse();
            capturedSettings.MaxParallelConnections.ShouldBe(5);
            
            // Check that UpdatedAt is set to a recent time (within last 5 seconds)
            var timeDifference = Math.Abs((DateTime.UtcNow - capturedSettings.UpdatedAt).TotalSeconds);
            timeDifference.ShouldBeLessThan(5);
            
            _mockDb.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveSettingsAsync_UpdatesExistingSettingsRecord_WhenRecordExists()
        {
            // Arrange
            var existingSettings = new Settings { Id = 1 };
            _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingSettings);
            
            var settingsToSave = new AppSettings
            {
                ImapSettings = new ImapSettingsDto("updated.server.com", 25, "updated.user", "", true, 10),
                Buckets = new List<BucketDto>(),
                FolderMappings = new List<FolderMappingDto>()
            };
            
            _mockDb.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            
            var service = CreateService();

            // Act
            await service.SaveSettingsAsync(settingsToSave);

            // Assert
            existingSettings.ImapServer.ShouldBe("updated.server.com");
            existingSettings.ImapPort.ShouldBe(25);
            existingSettings.ImapUsername.ShouldBe("updated.user");
            existingSettings.ImapPassword.ShouldBe(string.Empty); // Empty password doesn't update
            existingSettings.ImapUseSsl.ShouldBeTrue();
            existingSettings.MaxParallelConnections.ShouldBe(10);
            
            // Check that UpdatedAt is set to a recent time (within last 5 seconds)
            var timeDifference = Math.Abs((DateTime.UtcNow - existingSettings.UpdatedAt).TotalSeconds);
            timeDifference.ShouldBeLessThan(5);
            
            _mockDb.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveSettingsAsync_PreservesExistingPassword_WhenNewPasswordIsEmpty()
        {
            // Arrange
            var existingSettings = new Settings 
            { 
                Id = 1,
                ImapPassword = "existing.secret.password" 
            };
            _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingSettings);
            
            var settingsToSave = new AppSettings
            {
                ImapSettings = new ImapSettingsDto("server.com", 993, "user", "", true, 4), // Empty password
                Buckets = new List<BucketDto>(),
                FolderMappings = new List<FolderMappingDto>()
            };
            
            _mockDb.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            
            var service = CreateService();

            // Act
            await service.SaveSettingsAsync(settingsToSave);

            // Assert
            existingSettings.ImapPassword.ShouldBe("existing.secret.password"); // Should be preserved
            
            _mockDb.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveSettingsAsync_ValidatesMaxParallelConnections_Range()
        {
            // Arrange
            _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Settings?)null);
            
            var testCases = new[]
            {
                new { Input = 0, Expected = 4 },    // Below minimum -> default
                new { Input = 1, Expected = 1 },    // Minimum valid
                new { Input = 10, Expected = 10 },  // Middle valid
                new { Input = 20, Expected = 20 },  // Maximum valid
                new { Input = 21, Expected = 4 },   // Above maximum -> default
                new { Input = 100, Expected = 4 }   // Way above -> default
            };
            
            foreach (var testCase in testCases)
            {
                // Arrange for this test case
                _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Settings?)null);
                
                Settings? capturedSettings = null;
                _mockDb.Setup(db => db.Settings.Add(It.IsAny<Settings>()))
                    .Callback<Settings>(s => capturedSettings = s);
                _mockDb.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
                
                var settingsToSave = new AppSettings
                {
                    ImapSettings = new ImapSettingsDto("test.com", 993, "test", "pass", true, testCase.Input),
                    Buckets = new List<BucketDto>(),
                    FolderMappings = new List<FolderMappingDto>()
                };
                
                var service = CreateService();

                // Act
                await service.SaveSettingsAsync(settingsToSave);

                // Assert
                capturedSettings!.MaxParallelConnections.ShouldBe(testCase.Expected);
                
                // Clear setup for next iteration
                _mockDb.Reset();
                _mockDb.Setup(db => db.Settings.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Settings?)null);
                _mockDb.Setup(db => db.Settings.Add(It.IsAny<Settings>()))
                    .Callback<Settings>(s => capturedSettings = s);
                _mockDb.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
            }
        }
    }
}