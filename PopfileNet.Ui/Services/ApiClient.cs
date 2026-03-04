using System.Text.Json;

namespace PopfileNet.Ui.Services;

public class ApiClient(HttpClient http) : IApiClient
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
