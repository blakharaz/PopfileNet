using PopfileNet.Ui.Services;

namespace PopfileNet.Ui.UnitTests.TestHelpers;

/// <summary>
/// Mock implementation of IApiClient for testing purposes.
/// Provides default empty responses for all API methods that tests can override as needed.
/// </summary>
public class MockApiClient : IApiClient
{
    public virtual Task<PagedResponse<AccountDto>?> GetAccountsAsync(int page = 1, int pageSize = 20) =>
        Task.FromResult<PagedResponse<AccountDto>?>(new PagedResponse<AccountDto>
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        });

    public virtual Task<PagedResponse<BucketDto>?> GetCategoriesAsync(int page = 1, int pageSize = 20) =>
        Task.FromResult<PagedResponse<BucketDto>?>(new PagedResponse<BucketDto>
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        });

    public virtual Task<AppSettingsDto?> GetSettingsAsync() =>
        Task.FromResult<AppSettingsDto?>(new AppSettingsDto
        {
            ImapSettings = new ImapSettingsDto(),
            Buckets = [],
            FolderMappings = []
        });

    public virtual Task<bool> SaveSettingsAsync(AppSettingsDto settings) => Task.FromResult(true);
    public virtual Task<bool> TestConnectionAsync() => Task.FromResult(true);

    public virtual Task<PagedResponse<BucketDto>?> GetBucketsAsync(int page = 1, int pageSize = 20) =>
        Task.FromResult<PagedResponse<BucketDto>?>(new PagedResponse<BucketDto>
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        });

    public virtual Task<BucketDto?> CreateBucketAsync(BucketDto bucket) =>
        Task.FromResult<BucketDto?>(bucket);

    public virtual Task<BucketDto?> UpdateBucketAsync(BucketDto bucket) =>
        Task.FromResult<BucketDto?>(bucket);

    public virtual Task DeleteBucketAsync(string id) => Task.CompletedTask;

    public virtual Task<SyncJobResult?> SyncEmailsAsync() =>
        Task.FromResult<SyncJobResult?>(new SyncJobResult(true, "Sync completed", 0));

    public virtual Task<bool> UpdateFolderListAsync() => Task.FromResult(true);

    public virtual Task<PagedResponse<FolderDto>?> GetFoldersAsync(int page = 1, int pageSize = 20) =>
        Task.FromResult<PagedResponse<FolderDto>?>(new PagedResponse<FolderDto>
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        });

    public virtual Task<PagedResponse<EmailDto>?> GetMailsAsync(int page = 1, int pageSize = 20) =>
        Task.FromResult<PagedResponse<EmailDto>?>(new PagedResponse<EmailDto>
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            TotalPages = 1,
            HasPrevious = false,
            HasNext = false,
            IsSuccess = true
        });

    public virtual Task<EmailDetailDto?> GetMailByIdAsync(string id) => Task.FromResult<EmailDetailDto?>(null);

    public virtual Task<ClassifierStatus?> GetClassifierStatusAsync() =>
        Task.FromResult<ClassifierStatus?>(new ClassifierStatus(false, 0));

    public virtual Task<bool> TrainClassifierAsync() => Task.FromResult(true);

    public virtual Task<PredictionResult?> PredictAsync(string emailId) => Task.FromResult<PredictionResult?>(null);

     public virtual Task<IReadOnlyList<FolderMappingDto>?> GetFolderMappingsAsync() =>
         Task.FromResult<IReadOnlyList<FolderMappingDto>?>([]);

    public virtual Task SetFolderMappingAsync(string folderName, string? bucketId) => Task.CompletedTask;

    public virtual Task RemoveFolderMappingAsync(string folderName) => Task.CompletedTask;
}
