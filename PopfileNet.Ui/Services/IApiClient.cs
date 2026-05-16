namespace PopfileNet.Ui.Services;

public interface IApiClient
{
    Task<PagedResponse<AccountDto>?> GetAccountsAsync(int page = 1, int pageSize = 20);
    Task<PagedResponse<BucketDto>?> GetCategoriesAsync(int page = 1, int pageSize = 20);
    Task<AppSettingsDto?> GetSettingsAsync();
    Task<bool> SaveSettingsAsync(AppSettingsDto settings);
    Task<bool> TestConnectionAsync();
    Task<PagedResponse<BucketDto>?> GetBucketsAsync(int page = 1, int pageSize = 20);
    Task<BucketDto?> CreateBucketAsync(BucketDto bucket);
    Task<BucketDto?> UpdateBucketAsync(BucketDto bucket);
    Task DeleteBucketAsync(string id);
    Task<SyncJobResult?> SyncEmailsAsync();
    Task<bool> UpdateFolderListAsync();
    Task<PagedResponse<FolderDto>?> GetFoldersAsync(int page = 1, int pageSize = 20);
    Task<IReadOnlyList<FolderMappingDto>?> GetFolderMappingsAsync();
    Task SetFolderMappingAsync(string folderName, string? bucketId);
    Task RemoveFolderMappingAsync(string folderName);
    Task<PagedResponse<EmailDto>?> GetMailsAsync(int page = 1, int pageSize = 20);
    Task<EmailDetailDto?> GetMailByIdAsync(string id);
    Task<ClassifierStatus?> GetClassifierStatusAsync();
    Task<bool> TrainClassifierAsync();
    Task<PredictionResult?> PredictAsync(string emailId);
}
