using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Models;
using PopfileNet.Backend.Services;
using PopfileNet.Common;
using PopfileNet.Database;
using PopfileNet.Imap.Services;
using Shouldly;
using Xunit;

namespace PopfileNet.Backend.UnitTests;

public class SettingsGroupExtensionsTests
{
    private static PopfileNetDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PopfileNetDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PopfileNetDbContext(options);
    }

    [Fact]
    public async Task TestConnectionAsync_ReturnsBadRequest_WhenNotConfigured()
    {
        var mockImapService = new Mock<IImapService>();
        mockImapService.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(false);

        var result = await SettingsGroupExtensions.TestConnectionAsync(mockImapService.Object);

        result.ShouldBeOfType<BadRequest<ApiResponse<bool>>>();
    }

    [Fact]
    public async Task TestConnectionAsync_ReturnsOk_WhenConfiguredAndSuccess()
    {
        var mockImapService = new Mock<IImapService>();
        mockImapService.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);
        mockImapService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(true);

        var result = await SettingsGroupExtensions.TestConnectionAsync(mockImapService.Object);

        result.ShouldBeOfType<Ok<ApiResponse<bool>>>();
    }

    [Fact]
    public async Task TestConnectionAsync_ReturnsOk_WhenConfiguredAndFailure()
    {
        var mockImapService = new Mock<IImapService>();
        mockImapService.Setup(x => x.IsConfiguredAsync()).ReturnsAsync(true);
        mockImapService.Setup(x => x.TestConnectionAsync()).ReturnsAsync(false);

        var result = await SettingsGroupExtensions.TestConnectionAsync(mockImapService.Object);

        result.ShouldBeOfType<Ok<ApiResponse<bool>>>();
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsOk_WithSettings()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings());
        var mockLogger = new Mock<ILogger<Program>>();

        var result = await SettingsGroupExtensions.GetSettingsAsync(mockSettingsService.Object, mockLogger.Object);

        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Value.ShouldNotBeNull();
        result.Value.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task GetSettingsAsync_ThrowsException_WhenServiceFails()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));
        var mockLogger = new Mock<ILogger<Program>>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SettingsGroupExtensions.GetSettingsAsync(mockSettingsService.Object, mockLogger.Object));
    }

    [Fact]
    public async Task SaveSettingsAsync_ReturnsOk_OnSuccess()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await SettingsGroupExtensions.SaveSettingsAsync(new AppSettings(), mockSettingsService.Object);

        result.ShouldBeOfType<Ok<ApiResponse<bool>>>();
    }

    [Fact]
    public async Task GetBucketsAsync_ReturnsPagedResults()
    {
        await using var context = CreateInMemoryContext();
        context.Buckets.AddRange(
            new Bucket { Id = "1", Name = "Bucket1", Description = "Desc1" },
            new Bucket { Id = "2", Name = "Bucket2", Description = "Desc2" },
            new Bucket { Id = "3", Name = "Bucket3" }
        );
        await context.SaveChangesAsync();

        var result = await SettingsGroupExtensions.GetBucketsAsync(context, 1, 20);

        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Value.ShouldNotBeNull();
        result.Value.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count().ShouldBe(3);
    }

    [Fact]
    public async Task GetBucketsAsync_RespectsPageSize()
    {
        await using var context = CreateInMemoryContext();
        for (var i = 0; i < 10; i++)
        {
            context.Buckets.Add(new Bucket { Id = Guid.NewGuid().ToString(), Name = $"Bucket{i}" });
        }
        await context.SaveChangesAsync();

        var result = await SettingsGroupExtensions.GetBucketsAsync(context, 1, 5);

        result.Value.ShouldNotBeNull();
        result.Value.Items.ShouldNotBeNull();
        result.Value.Items.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetBucketsAsync_CapsPageSizeAt100()
    {
        await using var context = CreateInMemoryContext();
        for (var i = 0; i < 10; i++)
        {
            context.Buckets.Add(new Bucket { Id = Guid.NewGuid().ToString(), Name = $"Bucket{i}" });
        }
        await context.SaveChangesAsync();

        var result = await SettingsGroupExtensions.GetBucketsAsync(context, 1, 200);

        result.Value.ShouldNotBeNull();
        result.Value.Items.ShouldNotBeNull();
        result.Value.Items.Count().ShouldBe(10);
        result.Value.PageSize.ShouldBe(100);
    }

    [Fact]
    public async Task GetBucketsAsync_HandlesEmptyDescription()
    {
        await using var context = CreateInMemoryContext();
        context.Buckets.Add(new Bucket { Id = "1", Name = "Bucket1" });
        await context.SaveChangesAsync();

        var result = await SettingsGroupExtensions.GetBucketsAsync(context, 1, 20);

        result.Value.ShouldNotBeNull();
        result.Value.Items.ShouldNotBeNull();
        result.Value.Items.First().Description.ShouldBe("");
    }

    [Fact]
    public async Task CreateBucketAsync_CreatesBucketWithGeneratedId()
    {
        await using var context = CreateInMemoryContext();
        var bucket = new BucketDto("", "New Bucket", "Description");

        var result = await SettingsGroupExtensions.CreateBucketAsync(bucket, context);

        result.ShouldBeOfType<Created<ApiResponse<BucketDto>>>();
        var created = context.Buckets.First();
        created.Name.ShouldBe("New Bucket");
        created.Description.ShouldBe("Description");
        created.Id.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task CreateBucketAsync_UsesProvidedId()
    {
        await using var context = CreateInMemoryContext();
        var bucket = new BucketDto("custom-id", "Custom Bucket", "");

        var result = await SettingsGroupExtensions.CreateBucketAsync(bucket, context);

        var created = context.Buckets.First();
        created.Id.ShouldBe("custom-id");
    }

    [Fact]
    public async Task UpdateBucketAsync_UpdatesExistingBucket()
    {
        await using var context = CreateInMemoryContext();
        var bucket = new Bucket { Id = "test-id", Name = "Old Name", Description = "Old Desc" };
        context.Buckets.Add(bucket);
        await context.SaveChangesAsync();

        var updateDto = new BucketDto("test-id", "New Name", "New Desc");
        var result = await SettingsGroupExtensions.UpdateBucketAsync("test-id", updateDto, context);

        result.ShouldBeOfType<Ok<ApiResponse<BucketDto>>>();
        var updated = context.Buckets.First(b => b.Id == "test-id");
        updated.Name.ShouldBe("New Name");
        updated.Description.ShouldBe("New Desc");
    }

    [Fact]
    public async Task UpdateBucketAsync_ReturnsNotFound_WhenBucketDoesNotExist()
    {
        await using var context = CreateInMemoryContext();
        var updateDto = new BucketDto("nonexistent", "Name", "Desc");

        var result = await SettingsGroupExtensions.UpdateBucketAsync("nonexistent", updateDto, context);

        result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task DeleteBucketAsync_DeletesBucket()
    {
        await using var context = CreateInMemoryContext();
        var bucket = new Bucket { Id = "to-delete", Name = "Delete Me" };
        context.Buckets.Add(bucket);
        await context.SaveChangesAsync();

        var result = await SettingsGroupExtensions.DeleteBucketAsync("to-delete", context);

        result.ShouldBeOfType<NoContent>();
        context.Buckets.Count().ShouldBe(0);
    }

    [Fact]
    public async Task DeleteBucketAsync_ReturnsNotFound_WhenBucketDoesNotExist()
    {
        await using var context = CreateInMemoryContext();

        var result = await SettingsGroupExtensions.DeleteBucketAsync("nonexistent", context);

        result.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetFolderMappingsAsync_ReturnsOk_WithMappings()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.GetFolderMappingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FolderMappingDto> { new("Inbox", "bucket-1") });

        var result = await SettingsGroupExtensions.GetFolderMappingsAsync(mockSettingsService.Object);

        result.StatusCode.ShouldBe(StatusCodes.Status200OK);
        result.Value.ShouldNotBeNull();
        result.Value.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldNotBeNull();
        result.Value.Value.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetFolderMappingsAsync_ReturnsEmptyList_WhenNoMappings()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.GetFolderMappingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await SettingsGroupExtensions.GetFolderMappingsAsync(mockSettingsService.Object);

        result.Value.ShouldNotBeNull();
        result.Value.Value.ShouldNotBeNull();
        result.Value.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetFolderMappingAsync_ReturnsOk_OnSuccess()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.SetFolderMappingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockLogger = new Mock<ILogger<Program>>();

        var mapping = new FolderMappingDto("TestFolder", "bucket-id");
        var result = await SettingsGroupExtensions.SetFolderMappingAsync(mapping, mockSettingsService.Object, mockLogger.Object);

        result.ShouldBeOfType<Ok<ApiResponse<FolderMappingDto>>>();
    }

    [Fact]
    public async Task SetFolderMappingAsync_ReturnsBadRequest_OnArgumentException()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.SetFolderMappingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid folder name"));
        var mockLogger = new Mock<ILogger<Program>>();

        var mapping = new FolderMappingDto("", "bucket-id");
        var result = await SettingsGroupExtensions.SetFolderMappingAsync(mapping, mockSettingsService.Object, mockLogger.Object);

        result.ShouldBeOfType<BadRequest<ApiResponse<FolderMappingDto>>>();
    }

    [Fact]
    public async Task SetFolderMappingAsync_ReturnsNotFound_WhenFolderNotFound()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.SetFolderMappingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Folder 'TestFolder' not found"));
        var mockLogger = new Mock<ILogger<Program>>();

        var mapping = new FolderMappingDto("TestFolder", "bucket-id");
        var result = await SettingsGroupExtensions.SetFolderMappingAsync(mapping, mockSettingsService.Object, mockLogger.Object);

        result.ShouldBeOfType<NotFound<ApiResponse<FolderMappingDto>>>();
    }

    [Fact]
    public async Task SetFolderMappingAsync_ReturnsNotFound_WhenBucketNotFound()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.SetFolderMappingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Bucket with ID 'nonexistent' not found"));
        var mockLogger = new Mock<ILogger<Program>>();

        var mapping = new FolderMappingDto("TestFolder", "nonexistent");
        var result = await SettingsGroupExtensions.SetFolderMappingAsync(mapping, mockSettingsService.Object, mockLogger.Object);

        result.ShouldBeOfType<NotFound<ApiResponse<FolderMappingDto>>>();
    }

    [Fact]
    public async Task SetFolderMappingAsync_ReturnsBadRequest_OnGenericException()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.SetFolderMappingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        var mockLogger = new Mock<ILogger<Program>>();

        var mapping = new FolderMappingDto("TestFolder", "bucket-id");
        var result = await SettingsGroupExtensions.SetFolderMappingAsync(mapping, mockSettingsService.Object, mockLogger.Object);

        result.ShouldBeOfType<BadRequest<ApiResponse<FolderMappingDto>>>();
    }

    [Fact]
    public async Task RemoveFolderMappingAsync_ReturnsNoContent_OnSuccess()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.RemoveFolderMappingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockLogger = new Mock<ILogger<Program>>();

        var result = await SettingsGroupExtensions.RemoveFolderMappingAsync("TestFolder", mockSettingsService.Object, mockLogger.Object);

        result.ShouldBeOfType<NoContent>();
    }

    [Fact]
    public async Task RemoveFolderMappingAsync_ReturnsBadRequest_OnArgumentException()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.RemoveFolderMappingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid folder name"));
        var mockLogger = new Mock<ILogger<Program>>();

        var result = await SettingsGroupExtensions.RemoveFolderMappingAsync("", mockSettingsService.Object, mockLogger.Object);

        result.ShouldBeOfType<BadRequest<ApiResponse<bool>>>();
    }

    [Fact]
    public async Task RemoveFolderMappingAsync_ReturnsNotFound_WhenFolderNotFound()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.RemoveFolderMappingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Folder 'TestFolder' not found"));
        var mockLogger = new Mock<ILogger<Program>>();

        var result = await SettingsGroupExtensions.RemoveFolderMappingAsync("TestFolder", mockSettingsService.Object, mockLogger.Object);

        result.ShouldBeOfType<NotFound<ApiResponse<bool>>>();
    }

    [Fact]
    public async Task RemoveFolderMappingAsync_ReturnsBadRequest_OnGenericException()
    {
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(x => x.RemoveFolderMappingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));
        var mockLogger = new Mock<ILogger<Program>>();

        var result = await SettingsGroupExtensions.RemoveFolderMappingAsync("TestFolder", mockSettingsService.Object, mockLogger.Object);

        result.ShouldBeOfType<BadRequest<ApiResponse<bool>>>();
    }
}
