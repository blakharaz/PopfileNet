namespace PopfileNet.Ui.Services;

public class ApiClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    // Settings
    public async Task<ImapSettings?> GetSettingsAsync() =>
        await _http.GetFromJsonAsync<ImapSettings>("/settings");

    public async Task SaveSettingsAsync(ImapSettings settings) =>
        await _http.PostAsJsonAsync("/settings", settings);

    public async Task<bool> TestConnectionAsync() =>
        (await _http.PostAsync("/settings/test-connection", null)).IsSuccessStatusCode;

    public async Task<List<Bucket>> GetBucketsAsync() =>
        await _http.GetFromJsonAsync<List<Bucket>>("/settings/buckets") ?? [];

    public async Task<Bucket> CreateBucketAsync(Bucket bucket) =>
        (await _http.PostAsJsonAsync("/settings/buckets", bucket)).Content.ReadFromJsonAsync<Bucket>().Result!;

    public async Task UpdateBucketAsync(Bucket bucket) =>
        await _http.PutAsJsonAsync($"/settings/buckets/{bucket.Id}", bucket);

    public async Task DeleteBucketAsync(Guid id) =>
        await _http.DeleteAsync($"/settings/buckets/{id}");

    // Mails
    public async Task<List<FolderInfo>> GetFoldersAsync() =>
        await _http.GetFromJsonAsync<List<FolderInfo>>("/mails/folders") ?? [];

    public async Task<MailStats> GetMailStatsAsync() =>
        await _http.GetFromJsonAsync<MailStats>("/mails/stats") ?? MailStats.Empty;

    public async Task<SyncResult> SyncFolderAsync(string folderName) =>
        (await _http.PostAsync($"/mails/sync/{folderName}", null)).Content.ReadFromJsonAsync<SyncResult>().Result ?? SyncResult.Empty;

    public async Task<List<EmailDto>> GetEmailsAsync(string folderName) =>
        await _http.GetFromJsonAsync<List<EmailDto>>($"/mails/{folderName}") ?? [];

    // Classifier
    public async Task<ClassifierStatus> GetClassifierStatusAsync() =>
        await _http.GetFromJsonAsync<ClassifierStatus>("/classifier/status") ?? ClassifierStatus.Empty;

    public async Task TrainClassifierAsync() =>
        await _http.PostAsync("/classifier/train", null);

    public async Task<PredictionResult> PredictAsync(string emailId) =>
        (await _http.PostAsJsonAsync("/classifier/predict", new { EmailId = emailId })).Content.ReadFromJsonAsync<PredictionResult>().Result ?? PredictionResult.Empty;
}

public record ImapSettings
{
    public string Server { get; init; } = "";
    public int Port { get; init; } = 993;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public bool UseSsl { get; init; } = true;
}

public record Bucket(Guid Id, string Name, string Description);

public record FolderInfo(string Name, int EmailCount);

public record MailStats(int TotalEmails, Dictionary<string, int> EmailsByFolder)
{
    public static MailStats Empty => new(0, []);
}

public record SyncResult(bool Success, string Message, int SyncedCount)
{
    public static SyncResult Empty => new(false, "", 0);
}

public record EmailDto(string Id, string Subject, string FromAddress, DateTime ReceivedDate, string BucketName);

public record ClassifierStatus(bool IsTrained, int TrainingDataCount)
{
    public static ClassifierStatus Empty => new(false, 0);
}

public record PredictionResult(string PredictedBucket, float Confidence, Dictionary<string, float> AllProbabilities)
{
    public static PredictionResult Empty => new("", 0, []);
}
