using System.Text.Json;

namespace PopfileNet.Ui.Services;

public class ApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private async Task<T?> GetAsync<T>(string requestUri) where T : class
    {
        var response = await _http.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
        return result?.Value;
    }

    private async Task<T?> PostAsync<T>(string requestUri, object? data = null) where T : class
    {
        var response = data == null
            ? await _http.PostAsync(requestUri, null)
            : await _http.PostAsJsonAsync(requestUri, data);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, JsonOptions);
        return apiResponse?.Value;
    }

    private async Task<bool> PostBoolAsync(string requestUri, object? data = null)
    {
        var response = data == null
            ? await _http.PostAsync(requestUri, null)
            : await _http.PostAsJsonAsync(requestUri, data);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions);
        return apiResponse?.Value ?? false;
    }

    private async Task<PagedResponse<T>?> GetPagedAsync<T>(string requestUri)
    {
        var response = await _http.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResponse<T>>(content, JsonOptions);
    }

    // Accounts
    public async Task<PagedResponse<AccountDto>?> GetAccountsAsync(int page = 1, int pageSize = 20) =>
        await GetPagedAsync<AccountDto>($"/accounts?page={page}&pageSize={pageSize}");

    // Categories
    public async Task<PagedResponse<BucketDto>?> GetCategoriesAsync(int page = 1, int pageSize = 20) =>
        await GetPagedAsync<BucketDto>($"/categories?page={page}&pageSize={pageSize}");

    // Settings
    public async Task<AppSettingsDto?> GetSettingsAsync() =>
        await GetAsync<AppSettingsDto>("/settings");

    public async Task<bool> SaveSettingsAsync(AppSettingsDto settings) =>
        await PostBoolAsync("/settings", settings);

    public async Task<bool> TestConnectionAsync() =>
        await PostBoolAsync("/settings/test-connection");

    public async Task<PagedResponse<BucketDto>?> GetBucketsAsync(int page = 1, int pageSize = 20) =>
        await GetPagedAsync<BucketDto>($"/settings/buckets?page={page}&pageSize={pageSize}");

    public async Task<BucketDto?> CreateBucketAsync(BucketDto bucket) =>
        await PostAsync<BucketDto>("/settings/buckets", bucket);

    public async Task<BucketDto?> UpdateBucketAsync(BucketDto bucket)
    {
        var response = await _http.PutAsJsonAsync($"/settings/buckets/{bucket.Id}", bucket);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<BucketDto>>(content, JsonOptions)?.Value;
    }

    public async Task DeleteBucketAsync(Guid id)
    {
        await _http.DeleteAsync($"/settings/buckets/{id}");
    }

    // Jobs
    public async Task<SyncJobResult?> SyncEmailsAsync() =>
        await PostAsync<SyncJobResult>("/jobs/sync");

    public async Task<bool> UpdateFolderListAsync() =>
        await PostBoolAsync("/jobs/update-folder-list");

    // Folders
    public async Task<PagedResponse<FolderDto>?> GetFoldersAsync(int page = 1, int pageSize = 20) =>
        await GetPagedAsync<FolderDto>($"/folders?page={page}&pageSize={pageSize}");

    // Mails
    public async Task<PagedResponse<EmailDto>?> GetMailsAsync(int page = 1, int pageSize = 20) =>
        await GetPagedAsync<EmailDto>($"/mails?page={page}&pageSize={pageSize}");

    public async Task<EmailDetailDto?> GetMailByIdAsync(string id) =>
        await GetAsync<EmailDetailDto>($"/mails/{id}");

    // Classifier
    public async Task<ClassifierStatus?> GetClassifierStatusAsync() =>
        await GetAsync<ClassifierStatus>("/classifier/status");

    public async Task<bool> TrainClassifierAsync() =>
        await PostBoolAsync("/classifier/train");

    public async Task<PredictionResult?> PredictAsync(string emailId) =>
        await PostAsync<PredictionResult>("/classifier/predict", new { EmailId = emailId });
}

public class ApiResponse<T>
{
    public T? Value { get; set; }
    public ApiError? Error { get; set; }
    public bool IsSuccess => Error == null;
}

public class ApiError
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public bool IsSuccess { get; set; }
    public ApiError? Error { get; set; }
}

public record AppSettingsDto
{
    public ImapSettingsDto? ImapSettings { get; init; }
    public List<BucketDto> Buckets { get; init; } = [];
    public List<FolderMappingDto> FolderMappings { get; init; } = [];
}

public record ImapSettingsDto
{
    public string Server { get; init; } = "";
    public int Port { get; init; } = 993;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public bool UseSsl { get; init; } = true;
}

public class BucketDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    
    public BucketDto() { }
    
    public BucketDto(Guid id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }
}

public record FolderMappingDto(string Name, Guid? BucketId);

public record FolderDto(Guid Id, string Name);

public record AccountDto(string Id, string Name, string Server, int Port, bool UseSsl);

public record EmailDto(string Id, string Subject, string FromAddress, DateTime ReceivedDate, string BucketName);

public record EmailDetailDto(string Id, string Subject, string FromAddress, string ToAddresses, DateTime ReceivedDate, string Body, string BucketName);

public record SyncJobResult(bool Success, string Message, int SyncedCount)
{
    public static SyncJobResult Empty => new(false, "", 0);
}

public record ClassifierStatus(bool IsTrained, int TrainingDataCount)
{
    public static ClassifierStatus Empty => new(false, 0);
}

public record PredictionResult(string PredictedBucket, float Confidence, Dictionary<string, float> AllProbabilities)
{
    public static PredictionResult Empty => new("", 0, []);
}
