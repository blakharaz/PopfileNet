using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using PopfileNet.Backend.Models;

namespace PopfileNet.FunctionalTests;

public class ApiHelper(HttpClient client, string connectionString)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<BucketDto> CreateBucketAsync(string name, string? description = null)
    {
        var bucket = new BucketDto(string.Empty, name, description ?? "");
        var response = await client.PostAsJsonAsync("/settings/buckets", bucket, JsonOptions);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BucketDto>>(JsonOptions);
        return result?.Value ?? throw new InvalidOperationException("Failed to create bucket");
    }

    public async Task<IReadOnlyList<FolderMappingDto>> GetFolderMappingsAsync()
    {
        var response = await client.GetAsync("/settings/folder-mappings");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<FolderMappingDto>>>(JsonOptions);
        return result?.Value ?? [];
    }

    public async Task SetFolderMappingAsync(string folderName, string bucketId)
    {
        var mapping = new FolderMappingDto(folderName, bucketId);
        var response = await client.PostAsJsonAsync("/settings/folder-mappings", mapping, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to set folder mapping: {response.StatusCode} - {body}");
        }
    }

    public async Task RemoveFolderMappingAsync(string folderName)
    {
        var response = await client.DeleteAsync($"/settings/folder-mappings/{folderName}");
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to remove folder mapping: {response.StatusCode} - {body}");
        }
    }

    public async Task<IReadOnlyList<BucketDto>> GetBucketsAsync()
    {
        var response = await client.GetAsync("/settings/buckets");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedApiResponse<BucketDto>>(JsonOptions);
        return result?.Items?.ToList() ?? [];
    }

    public async Task<string> CreateTestFolderAsync(string folderName)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO \"MailFolders\" (\"Id\", \"Name\", \"BucketId\") VALUES (@id, @name, NULL) ON CONFLICT (\"Name\") DO NOTHING",
            conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("name", folderName);
        await cmd.ExecuteNonQueryAsync();
        return folderName;
    }

    public async Task<string> CreateTestBucketAsync(string bucketName, string? description = null)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO \"Buckets\" (\"Id\", \"Name\", \"Description\") VALUES (@id, @name, @desc)",
            conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("name", bucketName);
        cmd.Parameters.AddWithValue("desc", description ?? string.Empty);
        await cmd.ExecuteNonQueryAsync();
        return id;
    }

    public async Task SeedDataAsync(int folderCount = 2, int bucketCount = 2)
    {
        for (var i = 1; i <= bucketCount; i++)
        {
            await CreateTestBucketAsync($"Bucket{i}", $"Test bucket {i}");
        }

        for (var i = 1; i <= folderCount; i++)
        {
            await CreateTestFolderAsync($"TestFolder{i}");
        }
    }

    public async Task CleanupTestDataAsync()
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        
        // Delete all test buckets and folders (they have unique names with GUIDs)
        // Folder mappings are stored in MailFolders table with BucketId column
        await using var cmd = new NpgsqlCommand(
            "UPDATE \"MailFolders\" SET \"BucketId\" = NULL; DELETE FROM \"MailFolders\"; DELETE FROM \"Buckets\";",
            conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
