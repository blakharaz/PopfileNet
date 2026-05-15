using System.Net;
using System.Text.Json;
using PopfileNet.Ui.Services;
using Shouldly;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Services;

public class ApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static HttpClient CreateMockClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        return new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost") };
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsSettings_OnSuccess()
    {
        var expectedSettings = new AppSettingsDto
        {
            ImapSettings = new ImapSettingsDto { Server = "imap.test.com", Port = 993 },
            Buckets = [],
            FolderMappings = []
        };
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<AppSettingsDto> { Value = expectedSettings };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.GetSettingsAsync();
        
        result.ShouldNotBeNull();
        result.ImapSettings.ShouldNotBeNull();
        result.ImapSettings.Server.ShouldBe("imap.test.com");
    }

    [Fact]
    public async Task SaveSettingsAsync_ReturnsTrue_OnSuccess()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<bool> { Value = true };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        var settings = new AppSettingsDto();
        
        var result = await apiClient.SaveSettingsAsync(settings);
        
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_ReturnsTrue_OnSuccess()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<bool> { Value = true };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.TestConnectionAsync();
        
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task GetBucketsAsync_ReturnsPagedBuckets_OnSuccess()
    {
        var expectedBuckets = new PagedResponse<BucketDto>
        {
            Items = [new BucketDto("1", "Work", "Work emails")],
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        };
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var content = JsonSerializer.Serialize(expectedBuckets, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.GetBucketsAsync();
        
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Work");
    }

    [Fact]
    public async Task CreateBucketAsync_ReturnsCreatedBucket_OnSuccess()
    {
        var expectedBucket = new BucketDto("new-id", "New Bucket", "Description");
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<BucketDto> { Value = expectedBucket };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        var bucket = new BucketDto("", "New Bucket", "Description");
        
        var result = await apiClient.CreateBucketAsync(bucket);
        
        result.ShouldNotBeNull();
        result.Id.ShouldBe("new-id");
        result.Name.ShouldBe("New Bucket");
    }

    [Fact]
    public async Task UpdateBucketAsync_ReturnsUpdatedBucket_OnSuccess()
    {
        var expectedBucket = new BucketDto("1", "Updated Name", "Updated Desc");
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<BucketDto> { Value = expectedBucket };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        var bucket = new BucketDto("1", "Updated Name", "Updated Desc");
        
        var result = await apiClient.UpdateBucketAsync(bucket);
        
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Name");
    }

    [Fact]
    public async Task DeleteBucketAsync_CompletesWithoutError_OnSuccess()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        
        var apiClient = new ApiClient(client);
        
        await apiClient.DeleteBucketAsync("bucket-id");
        
        // Verify no exception was thrown
        true.ShouldBeTrue();
    }

    [Fact]
    public async Task GetFolderMappingsAsync_ReturnsMappings_OnSuccess()
    {
        var expectedMappings = new List<FolderMappingDto>
        {
            new("Inbox", "bucket-1"),
            new("Archive", null)
        };
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<IReadOnlyList<FolderMappingDto>> { Value = expectedMappings };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.GetFolderMappingsAsync();
        
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Inbox");
        result[1].BucketId.ShouldBeNull();
    }

    [Fact]
    public async Task SetFolderMappingAsync_CompletesWithoutError_OnSuccess()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<object> { Value = new object() };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        await apiClient.SetFolderMappingAsync("Inbox", "bucket-1");
        
        // Verify no exception was thrown
        true.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveFolderMappingAsync_CompletesWithoutError_OnSuccess()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        
        var apiClient = new ApiClient(client);
        
        await apiClient.RemoveFolderMappingAsync("Inbox");
        
        // Verify no exception was thrown
        true.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAccountsAsync_ReturnsPagedAccounts_OnSuccess()
    {
        var expectedAccounts = new PagedResponse<AccountDto>
        {
            Items = [],
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        };
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var content = JsonSerializer.Serialize(expectedAccounts, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.GetAccountsAsync();
        
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsPagedCategories_OnSuccess()
    {
        var expectedCategories = new PagedResponse<BucketDto>
        {
            Items = [],
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        };
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var content = JsonSerializer.Serialize(expectedCategories, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.GetCategoriesAsync();
        
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task SyncEmailsAsync_ReturnsResult_OnSuccess()
    {
        var expectedResult = new SyncJobResult(true, "Synced", 10);
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<SyncJobResult> { Value = expectedResult };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.SyncEmailsAsync();
        
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.SyncedCount.ShouldBe(10);
    }

    [Fact]
    public async Task UpdateFolderListAsync_ReturnsTrue_OnSuccess()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<bool> { Value = true };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.UpdateFolderListAsync();
        
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task GetFoldersAsync_ReturnsPagedFolders_OnSuccess()
    {
        var expectedFolders = new PagedResponse<FolderDto>
        {
            Items = [],
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        };
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var content = JsonSerializer.Serialize(expectedFolders, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.GetFoldersAsync();
        
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task GetMailsAsync_ReturnsPagedMails_OnSuccess()
    {
        var expectedMails = new PagedResponse<EmailDto>
        {
            Items = [],
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        };
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var content = JsonSerializer.Serialize(expectedMails, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.GetMailsAsync();
        
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task GetMailByIdAsync_ReturnsMail_OnSuccess()
    {
        var expectedMail = new EmailDetailDto("mail-1", "Test Subject", "from@test.com", "to@test.com", DateTime.UtcNow, "Test Body", "");
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<EmailDetailDto> { Value = expectedMail };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.GetMailByIdAsync("mail-1");
        
        result.ShouldNotBeNull();
        result.Subject.ShouldBe("Test Subject");
    }

    [Fact]
    public async Task GetClassifierStatusAsync_ReturnsStatus_OnSuccess()
    {
        var expectedStatus = new ClassifierStatus(true, 100);
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<ClassifierStatus> { Value = expectedStatus };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.GetClassifierStatusAsync();
        
        result.ShouldNotBeNull();
        result.IsTrained.ShouldBeTrue();
        result.TrainingDataCount.ShouldBe(100);
    }

    [Fact]
    public async Task TrainClassifierAsync_ReturnsTrue_OnSuccess()
    {
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<bool> { Value = true };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.TrainClassifierAsync();
        
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task PredictAsync_ReturnsPrediction_OnSuccess()
    {
        var expectedResult = new PredictionResult("bucket-1", 0.95f, new Dictionary<string, float> { { "bucket-1", 0.95f } });
        
        var client = CreateMockClient(async (request, ct) =>
        {
            var response = new ApiResponse<PredictionResult> { Value = expectedResult };
            var content = JsonSerializer.Serialize(response, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
        });
        
        var apiClient = new ApiClient(client);
        
        var result = await apiClient.PredictAsync("mail-1");
        
        result.ShouldNotBeNull();
        result.PredictedBucket.ShouldBe("bucket-1");
        result.Confidence.ShouldBe(0.95f);
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _handler(request, cancellationToken);
    }
}
