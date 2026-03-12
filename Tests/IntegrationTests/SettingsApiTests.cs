using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PopfileNet.Backend;
using PopfileNet.Backend.Models;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("Database")]
public class SettingsApiTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private HttpClient? _client;

    public SettingsApiTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:popfilenet"] = _fixture.ConnectionString,
                        ["ImapSettings:Server"] = "imap.test.com",
                        ["ImapSettings:Port"] = "993",
                        ["ImapSettings:Username"] = "test@test.com",
                        ["ImapSettings:Password"] = "test",
                        ["ImapSettings:UseSsl"] = "true"
                    });
                });
            });

        _client = factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
    }

    [Fact]
    public async Task GetSettings_ReturnsCurrentSettings()
    {
        var response = await _client!.GetAsync("/settings");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<AppSettings>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveSettings_ReturnsSuccess()
    {
        var settings = new AppSettings
        {
            ImapSettings = new ImapSettingsDto
            {
                Server = "imap.test.com",
                Port = 993,
                Username = "test@test.com",
                Password = "test",
                UseSsl = true
            }
        };

        var response = await _client!.PostAsJsonAsync("/settings", settings);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TestConnection_WithoutConfiguration_ReturnsBadRequest()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:popfilenet"] = _fixture.ConnectionString
                    });
                });
            });

        using var client = factory.CreateClient();
        var response = await client.PostAsync("/settings/test-connection", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBuckets_ReturnsPagedResults()
    {
        var response = await _client!.GetAsync("/settings/buckets");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<BucketDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateBucket_ReturnsCreated()
    {
        var bucket = new BucketDto("", "Test Bucket", "Test Description");

        var response = await _client!.PostAsJsonAsync("/settings/buckets", bucket);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<BucketDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Value.ShouldNotBeNull();
        content.Value.Name.ShouldBe("Test Bucket");
    }

    [Fact]
    public async Task UpdateBucket_ReturnsSuccess()
    {
        var createResponse = await _client!.PostAsJsonAsync("/settings/buckets", 
            new BucketDto("", "Original Name", "Original Description"));
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BucketDto>>();
        
        var update = new BucketDto(created!.Value!.Id, "Updated Name", "Updated Description");
        var updateResponse = await _client!.PutAsJsonAsync($"/settings/buckets/{created.Value.Id}", update);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<BucketDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Value!.Name.ShouldBe("Updated Name");
    }

    [Fact]
    public async Task DeleteBucket_ReturnsNoContent()
    {
        var createResponse = await _client!.PostAsJsonAsync("/settings/buckets", 
            new BucketDto("", "To Delete", "Description"));
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BucketDto>>();
        
        var deleteResponse = await _client!.DeleteAsync($"/settings/buckets/{created!.Value!.Id}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
