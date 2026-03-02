namespace PopfileNet.Backend.Services;

public interface ISettingsService
{
    Task<Models.AppSettings> GetSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(Models.AppSettings settings, CancellationToken ct = default);
}
