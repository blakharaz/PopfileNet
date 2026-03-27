using Shouldly;
using Microsoft.EntityFrameworkCore;
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
        private static PopfileNetDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new PopfileNetDbContext(options);
        }

        private static SettingsService CreateService(PopfileNetDbContext context, ImapSettings defaults)
        {
            return new SettingsService(context, defaults);
        }

        [Fact]
        public async Task GetSettingsAsync_ReturnsSettingsWithDefaults_WhenNoDbRecordExists()
        {
            // Arrange
            await using var context = CreateInMemoryContext();
            var defaults = new ImapSettings
            {
                Server = "default.server.com",
                Port = 993,
                Username = "default.user",
                Password = "default.password",
                UseSsl = true,
                MaxParallelConnections = 4
            };
            
            var service = CreateService(context, defaults);

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
            await using var context = CreateInMemoryContext();
            var defaults = new ImapSettings
            {
                Server = "default.server.com",
                Port = 993,
                Username = "default.user",
                Password = "default.password",
                UseSsl = true,
                MaxParallelConnections = 4
            };
            
            // Add a settings record to the database
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
            
            context.Settings.Add(dbSettings);
            context.Buckets.AddRange(dbBuckets);
            context.MailFolders.AddRange(dbFolders);
            await context.SaveChangesAsync();
            
            var service = CreateService(context, defaults);

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
            await using var context = CreateInMemoryContext();
            var defaults = new ImapSettings
            {
                Server = "default.server.com",
                Port = 993,
                Username = "default.user",
                Password = "default.password",
                UseSsl = true,
                MaxParallelConnections = 4
            };
            
            var service = CreateService(context, defaults);

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
            await using var context = CreateInMemoryContext();
            var defaults = new ImapSettings
            {
                Server = "default.server.com",
                Port = 993,
                Username = "default.user",
                Password = "default.password",
                UseSsl = true,
                MaxParallelConnections = 4
            };
            
            // Add a settings record to the database
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
            
            context.Settings.Add(dbSettings);
            await context.SaveChangesAsync();
            
            var service = CreateService(context, defaults);

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
            await using var context = CreateInMemoryContext();
            var defaults = new ImapSettings
            {
                Server = "default.server.com",
                Port = 993,
                Username = "default.user",
                Password = "default.password",
                UseSsl = true,
                MaxParallelConnections = 4
            };
            
             var settingsToSave = new AppSettings
             {
                 ImapSettings = new ImapSettingsDto("new.server.com", 587, "new.user", "new.password", false, 5),
                 Buckets = [],
                 FolderMappings = []
             };
            
            var service = CreateService(context, defaults);

            // Act
            await service.SaveSettingsAsync(settingsToSave);

            // Assert
            var savedSettings = await context.Settings.FindAsync(1);
            savedSettings.ShouldNotBeNull();
            var saved = savedSettings!;
            saved.ImapServer.ShouldBe("new.server.com");
            saved.ImapPort.ShouldBe(587);
            saved.ImapUsername.ShouldBe("new.user");
            saved.ImapPassword.ShouldBe("new.password");
            saved.ImapUseSsl.ShouldBeFalse();
            saved.MaxParallelConnections.ShouldBe(5);
            
            // Check that UpdatedAt is set to a recent time (within last 5 seconds)
            var timeDifference = Math.Abs((DateTime.UtcNow - saved.UpdatedAt).TotalSeconds);
            timeDifference.ShouldBeLessThan(5);
        }

        [Fact]
        public async Task SaveSettingsAsync_UpdatesExistingSettingsRecord_WhenRecordExists()
        {
            // Arrange
            await using var context = CreateInMemoryContext();
            var defaults = new ImapSettings
            {
                Server = "default.server.com",
                Port = 993,
                Username = "default.user",
                Password = "default.password",
                UseSsl = true,
                MaxParallelConnections = 4
            };
            
            // Add an existing settings record
            var existingSettings = new Settings { Id = 1 };
            context.Settings.Add(existingSettings);
            await context.SaveChangesAsync();
            
             var settingsToSave = new AppSettings
             {
                 ImapSettings = new ImapSettingsDto("updated.server.com", 25, "updated.user", "", true, 10),
                 Buckets = [],
                 FolderMappings = []
             };
            
            var service = CreateService(context, defaults);

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
        }

        [Fact]
        public async Task SaveSettingsAsync_PreservesExistingPassword_WhenNewPasswordIsEmpty()
        {
            // Arrange
            await using var context = CreateInMemoryContext();
            var defaults = new ImapSettings
            {
                Server = "default.server.com",
                Port = 993,
                Username = "default.user",
                Password = "default.password",
                UseSsl = true,
                MaxParallelConnections = 4
            };
            
            // Add an existing settings record with a password
            var existingSettings = new Settings 
            { 
                Id = 1,
                ImapPassword = "existing.secret.password" 
            };
            context.Settings.Add(existingSettings);
            await context.SaveChangesAsync();
            
            var settingsToSave = new AppSettings
            {
                ImapSettings = new ImapSettingsDto("server.com", 993, "user", "", true, 4), // Empty password
                Buckets = new List<BucketDto>(),
                FolderMappings = new List<FolderMappingDto>()
            };
            
            var service = CreateService(context, defaults);

            // Act
            await service.SaveSettingsAsync(settingsToSave);

            // Assert
            existingSettings.ImapPassword.ShouldBe("existing.secret.password"); // Should be preserved
        }

         [Fact]
         public async Task SaveSettingsAsync_ValidatesMaxParallelConnections_Range()
         {
             // Arrange
             await using var context = CreateInMemoryContext();
             var defaults = new ImapSettings
             {
                 Server = "default.server.com",
                 Port = 993,
                 Username = "default.user",
                 Password = "default.password",
                 UseSsl = true,
                 MaxParallelConnections = 4
             };
             
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
                 await using var testContext = CreateInMemoryContext();
                  var settingsToSave = new AppSettings
                  {
                      ImapSettings = new ImapSettingsDto("test.com", 993, "test", "pass", true, testCase.Input),
                      Buckets = [],
                      FolderMappings = []
                  };
                 
                 var service = CreateService(testContext, defaults);
 
                 // Act
                 await service.SaveSettingsAsync(settingsToSave);
 
                 // Assert
                 var savedSettings = await testContext.Settings.FindAsync(1);
                 savedSettings.ShouldNotBeNull();
                 savedSettings.MaxParallelConnections.ShouldBe(testCase.Expected);
             }
         }

         [Fact]
         public async Task GetFolderMappingsAsync_ReturnsEmptyList_WhenNoFoldersExist()
         {
             // Arrange
             await using var context = CreateInMemoryContext();
             var defaults = new ImapSettings
             {
                 Server = "default.server.com",
                 Port = 993,
                 Username = "default.user",
                 Password = "default.password",
                 UseSsl = true,
                 MaxParallelConnections = 4
             };
             
             var service = CreateService(context, defaults);
 
             // Act
             var result = await service.GetFolderMappingsAsync();
 
             // Assert
             result.ShouldNotBeNull();
             result.ShouldBeEmpty();
         }
 
          [Fact]
          public async Task GetFolderMappingsAsync_ReturnsCorrectMappings_WhenFoldersExist()
          {
              // Arrange
              await using var context = CreateInMemoryContext();
              var defaults = new ImapSettings
              {
                  Server = "default.server.com",
                  Port = 993,
                  Username = "default.user",
                  Password = "default.password",
                  UseSsl = true,
                  MaxParallelConnections = 4
              };
              
              // Add folders with different bucket assignments
              var dbFolders = new List<MailFolder>
              {
                  new MailFolder { Id = "folder1", Name = "Inbox", BucketId = "bucket1" },
                  new MailFolder { Id = "folder2", Name = "Work", BucketId = "bucket2" },
                  new MailFolder { Id = "folder3", Name = "Archive", BucketId = null } // No bucket assignment
              };
              
              context.MailFolders.AddRange(dbFolders);
              await context.SaveChangesAsync();
              
              var service = CreateService(context, defaults);
              
              // Act
              var result = await service.GetFolderMappingsAsync();
              
              // Assert
              result.ShouldNotBeNull();
              result.Count.ShouldBe(3);
              
              // Find each folder mapping by name and verify
              var inboxMapping = result.FirstOrDefault(f => f.Name == "Inbox");
              inboxMapping.ShouldNotBeNull();
              inboxMapping.BucketId.ShouldBe("bucket1");
              
              var workMapping = result.FirstOrDefault(f => f.Name == "Work");
              workMapping.ShouldNotBeNull();
              workMapping.BucketId.ShouldBe("bucket2");
              
              var archiveMapping = result.FirstOrDefault(f => f.Name == "Archive");
              archiveMapping.ShouldNotBeNull();
              archiveMapping.BucketId.ShouldBeNull();
          }

          [Fact]
          public async Task SetFolderMappingAsync_SetsFolderToBucket_WhenFolderAndBucketExist()
          {
              // Arrange
              await using var context = CreateInMemoryContext();
              var defaults = new ImapSettings
              {
                  Server = "default.server.com",
                  Port = 993,
                  Username = "default.user",
                  Password = "default.password",
                  UseSsl = true,
                  MaxParallelConnections = 4
              };
              
              var folder = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "TestFolder" };
              var bucket = new Bucket { Id = Guid.NewGuid().ToString(), Name = "TestBucket" };
              
              context.MailFolders.Add(folder);
              context.Buckets.Add(bucket);
              await context.SaveChangesAsync();
              
              var service = CreateService(context, defaults);
              
              // Act
              await service.SetFolderMappingAsync(folder.Name, bucket.Id);
              
              // Assert
              var updatedFolder = await context.MailFolders.FirstOrDefaultAsync(f => f.Name == folder.Name);
              updatedFolder.ShouldNotBeNull();
              updatedFolder.BucketId.ShouldBe(bucket.Id);
          }

          [Fact]
          public async Task SetFolderMappingAsync_SetsFolderToNull_WhenBucketIdIsNull()
          {
              // Arrange
              await using var context = CreateInMemoryContext();
              var defaults = new ImapSettings
              {
                  Server = "default.server.com",
                  Port = 993,
                  Username = "default.user",
                  Password = "default.password",
                  UseSsl = true,
                  MaxParallelConnections = 4
              };
              
              var folder = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "TestFolder" };
              var bucket = new Bucket { Id = Guid.NewGuid().ToString(), Name = "TestBucket" };
              
              // Start with the folder assigned to a bucket
              folder.BucketId = bucket.Id;
              context.MailFolders.Add(folder);
              context.Buckets.Add(bucket);
              await context.SaveChangesAsync();
              
              var service = CreateService(context, defaults);
              
              // Act
              await service.SetFolderMappingAsync(folder.Name, null);
              
              // Assert
              var updatedFolder = await context.MailFolders.FirstOrDefaultAsync(f => f.Name == folder.Name);
              updatedFolder.ShouldNotBeNull();
              updatedFolder.BucketId.ShouldBeNull();
          }

          [Fact]
          public async Task SetFolderMappingAsync_ThrowsKeyNotFoundException_WhenFolderDoesNotExist()
          {
              // Arrange
              await using var context = CreateInMemoryContext();
              var defaults = new ImapSettings
              {
                  Server = "default.server.com",
                  Port = 993,
                  Username = "default.user",
                  Password = "default.password",
                  UseSsl = true,
                  MaxParallelConnections = 4
              };
              
              var bucket = new Bucket { Id = Guid.NewGuid().ToString(), Name = "TestBucket" };
              context.Buckets.Add(bucket);
              await context.SaveChangesAsync();
              
              var service = CreateService(context, defaults);
              
              // Act
              Func<Task> act = async () => await service.SetFolderMappingAsync("NonExistentFolder", bucket.Id);
              
              // Assert
              await act.ShouldThrowAsync<KeyNotFoundException>();
          }

          [Fact]
          public async Task SetFolderMappingAsync_ThrowsKeyNotFoundException_WhenBucketDoesNotExist()
          {
              // Arrange
              await using var context = CreateInMemoryContext();
              var defaults = new ImapSettings
              {
                  Server = "default.server.com",
                  Port = 993,
                  Username = "default.user",
                  Password = "default.password",
                  UseSsl = true,
                  MaxParallelConnections = 4
              };
              
              var folder = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "TestFolder" };
              context.MailFolders.Add(folder);
              await context.SaveChangesAsync();
              
              var service = CreateService(context, defaults);
              
              // Act
              Func<Task> act = async () => await service.SetFolderMappingAsync(folder.Name, Guid.NewGuid().ToString());
              
              // Assert
              await act.ShouldThrowAsync<KeyNotFoundException>();
          }

          [Fact]
          public async Task SetFolderMappingAsync_ThrowsArgumentException_WhenFolderNameIsNullOrEmpty()
          {
              // Arrange
              await using var context = CreateInMemoryContext();
              var defaults = new ImapSettings
              {
                  Server = "default.server.com",
                  Port = 993,
                  Username = "default.user",
                  Password = "default.password",
                  UseSsl = true,
                  MaxParallelConnections = 4
              };
              
              var service = CreateService(context, defaults);
              
              // Act & Assert
              await Assert.ThrowsAsync<ArgumentException>(() => service.SetFolderMappingAsync(null!, "bucketId"));
              await Assert.ThrowsAsync<ArgumentException>(() => service.SetFolderMappingAsync(string.Empty, "bucketId"));
              await Assert.ThrowsAsync<ArgumentException>(() => service.SetFolderMappingAsync("   ", "bucketId"));
          }

          [Fact]
          public async Task RemoveFolderMappingAsync_RemovesFolderMapping_WhenFolderExists()
          {
              // Arrange
              await using var context = CreateInMemoryContext();
              var defaults = new ImapSettings
              {
                  Server = "default.server.com",
                  Port = 993,
                  Username = "default.user",
                  Password = "default.password",
                  UseSsl = true,
                  MaxParallelConnections = 4
              };
              
              var folder = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "TestFolder" };
              var bucket = new Bucket { Id = Guid.NewGuid().ToString(), Name = "TestBucket" };
              
              // Start with the folder assigned to a bucket
              folder.BucketId = bucket.Id;
              context.MailFolders.Add(folder);
              context.Buckets.Add(bucket);
              await context.SaveChangesAsync();
              
              var service = CreateService(context, defaults);
              
              // Act
              await service.RemoveFolderMappingAsync(folder.Name);
              
              // Assert
              var updatedFolder = await context.MailFolders.FirstOrDefaultAsync(f => f.Name == folder.Name);
              updatedFolder.ShouldNotBeNull();
              updatedFolder.BucketId.ShouldBeNull();
          }

          [Fact]
          public async Task RemoveFolderMappingAsync_ThrowsKeyNotFoundException_WhenFolderDoesNotExist()
          {
              // Arrange
              await using var context = CreateInMemoryContext();
              var defaults = new ImapSettings
              {
                  Server = "default.server.com",
                  Port = 993,
                  Username = "default.user",
                  Password = "default.password",
                  UseSsl = true,
                  MaxParallelConnections = 4
              };
              
              var service = CreateService(context, defaults);
              
              // Act
              Func<Task> act = async () => await service.RemoveFolderMappingAsync("NonExistentFolder");
              
              // Assert
              await act.ShouldThrowAsync<KeyNotFoundException>();
          }

          [Fact]
          public async Task RemoveFolderMappingAsync_ThrowsArgumentException_WhenFolderNameIsNullOrEmpty()
          {
              // Arrange
              await using var context = CreateInMemoryContext();
              var defaults = new ImapSettings
              {
                  Server = "default.server.com",
                  Port = 993,
                  Username = "default.user",
                  Password = "default.password",
                  UseSsl = true,
                  MaxParallelConnections = 4
              };
              
              var service = CreateService(context, defaults);
              
              // Act & Assert
              await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveFolderMappingAsync(null!));
              await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveFolderMappingAsync(string.Empty));
              await Assert.ThrowsAsync<ArgumentException>(() => service.RemoveFolderMappingAsync("   "));
          }
      }
}