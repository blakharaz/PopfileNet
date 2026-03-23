using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PopfileNet.Backend.Models;
using PopfileNet.Common;
using PopfileNet.Database;
using Shouldly;
using Xunit;

namespace PopfileNet.IntegrationTests;

[Collection("DatabaseTests")]
public class SettingsApiTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    protected override Task SetupClientAsync()
    {
        Factory = CreateWebApplicationFactory(Fixture.ConnectionString);
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetSettings_ReturnsCurrentSettings()
    {
        var response = await Client.GetAsync("/settings");

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

        var response = await Client.PostAsJsonAsync("/settings", settings);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task TestConnection_WithConfiguration_ReturnsOk()
    {
        var response = await Client.PostAsync("/settings/test-connection", null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBuckets_ReturnsPagedResults()
    {
        var response = await Client.GetAsync("/settings/buckets");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PagedApiResponse<BucketDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateBucket_ReturnsCreated()
    {
        var bucket = new BucketDto("", "Test Bucket", "Test Description");

        var response = await Client.PostAsJsonAsync("/settings/buckets", bucket);

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
        var createResponse = await Client.PostAsJsonAsync("/settings/buckets", 
            new BucketDto("", "Original Name", "Original Description"));
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BucketDto>>();
        created.ShouldNotBeNull();
        
        var update = new BucketDto(created.Value!.Id, "Updated Name", "Updated Description");
        created.Value.ShouldNotBeNull();
        var updateResponse = await Client.PutAsJsonAsync($"/settings/buckets/{created.Value.Id}", update);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<BucketDto>>();
        content.ShouldNotBeNull();
        content.IsSuccess.ShouldBeTrue();
        content.Value!.Name.ShouldBe("Updated Name");
    }

     [Fact]
     public async Task DeleteBucket_ReturnsNoContent()
     {
         var createResponse = await Client.PostAsJsonAsync("/settings/buckets", 
             new BucketDto("", "To Delete", "Description"));
         var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BucketDto>>();
         created.ShouldNotBeNull();
         
         var deleteResponse = await Client.DeleteAsync($"/settings/buckets/{created.Value!.Id}");
 
         deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
     }

     [Fact]
     public async Task GetFolderMappings_ReturnsCurrentMappings()
     {
         // Add a folder to the database for testing
         await using var dbContext = Fixture.CreateDbContext();
         var folder = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "TestFolder" };
         dbContext.MailFolders.Add(folder);
         await dbContext.SaveChangesAsync();
         
         var response = await Client.GetAsync("/settings/folder-mappings");
 
         response.StatusCode.ShouldBe(HttpStatusCode.OK);
         
         var content = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<FolderMappingDto>>>();
         content.ShouldNotBeNull();
         content.IsSuccess.ShouldBeTrue();
         content.Value.ShouldNotBeNull();
         // Should have at least the folder we added
         content.Value.ShouldContain(m => m.Name == "TestFolder" && m.BucketId == null);
     }

     [Fact]
     public async Task SetFolderMapping_CreatesMapping_WhenFolderExists()
     {
         // Add a folder and a bucket to the database for testing
         await using var dbContext = Fixture.CreateDbContext();
         var folder = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "TestFolder" };
         var bucket = new Bucket { Id = Guid.NewGuid().ToString(), Name = "TestBucket" };
         dbContext.MailFolders.Add(folder);
         dbContext.Buckets.Add(bucket);
         await dbContext.SaveChangesAsync();
         
         var mapping = new FolderMappingDto(folder.Name, bucket.Id);
         var response = await Client.PostAsJsonAsync("/settings/folder-mappings", mapping);
 
         response.StatusCode.ShouldBe(HttpStatusCode.OK);
         
         var content = await response.Content.ReadFromJsonAsync<ApiResponse<FolderMappingDto>>();
         content.ShouldNotBeNull();
         content.IsSuccess.ShouldBeTrue();
         content.Value.ShouldNotBeNull();
         content.Value.Name.ShouldBe(folder.Name);
         content.Value.BucketId.ShouldBe(bucket.Id);
         
         // Verify the mapping was saved to the database
         await using var verifyContext = Fixture.CreateDbContext();
         var dbFolder = await verifyContext.MailFolders.FirstOrDefaultAsync(f => f.Name == folder.Name);
         dbFolder.ShouldNotBeNull();
         dbFolder.BucketId.ShouldBe(bucket.Id);
     }
     
     [Fact]
     public async Task SetFolderMapping_UpdatesMapping_WhenFolderExists()
     {
         // Add two buckets and a folder to the database for testing
         await using var dbContext = Fixture.CreateDbContext();
         var folder = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "TestFolder" };
         var bucket1 = new Bucket { Id = Guid.NewGuid().ToString(), Name = "TestBucket1" };
         var bucket2 = new Bucket { Id = Guid.NewGuid().ToString(), Name = "TestBucket2" };
         dbContext.MailFolders.Add(folder);
         dbContext.Buckets.AddRange(new[] { bucket1, bucket2 });
         await dbContext.SaveChangesAsync();
         
         // First, set the folder to bucket1
         var initialMapping = new FolderMappingDto(folder.Name, bucket1.Id);
         await Client.PostAsJsonAsync("/settings/folder-mappings", initialMapping);
         
         // Then update it to bucket2
         var updatedMapping = new FolderMappingDto(folder.Name, bucket2.Id);
         var response = await Client.PostAsJsonAsync("/settings/folder-mappings", updatedMapping);
 
         response.StatusCode.ShouldBe(HttpStatusCode.OK);
         
         var content = await response.Content.ReadFromJsonAsync<ApiResponse<FolderMappingDto>>();
         content.ShouldNotBeNull();
         content.IsSuccess.ShouldBeTrue();
         content.Value.ShouldNotBeNull();
         content.Value.Name.ShouldBe(folder.Name);
         content.Value.BucketId.ShouldBe(bucket2.Id);
         
         // Verify the mapping was updated in the database
         await using var verifyContext = Fixture.CreateDbContext();
         var dbFolder = await verifyContext.MailFolders.FirstOrDefaultAsync(f => f.Name == folder.Name);
         dbFolder.ShouldNotBeNull();
         dbFolder.BucketId.ShouldBe(bucket2.Id);
     }
     
     [Fact]
     public async Task SetFolderMapping_ReturnsBadRequest_WhenFolderDoesNotExist()
     {
         // Add a bucket to the database for testing
         await using var dbContext = Fixture.CreateDbContext();
         var bucket = new Bucket { Id = Guid.NewGuid().ToString(), Name = "TestBucket" };
         dbContext.Buckets.Add(bucket);
         await dbContext.SaveChangesAsync();
         
         var mapping = new FolderMappingDto("NonExistentFolder", bucket.Id);
         var response = await Client.PostAsJsonAsync("/settings/folder-mappings", mapping);
 
         response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
     }
     
     [Fact]
     public async Task SetFolderMapping_ReturnsBadRequest_WhenBucketDoesNotExist()
     {
         // Add a folder to the database for testing
         await using var dbContext = Fixture.CreateDbContext();
         var folder = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "TestFolder" };
         dbContext.MailFolders.Add(folder);
         await dbContext.SaveChangesAsync();
         
         var mapping = new FolderMappingDto(folder.Name, Guid.NewGuid().ToString()); // Non-existent bucket ID
         var response = await Client.PostAsJsonAsync("/settings/folder-mappings", mapping);
 
         response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
     }
     
     [Fact]
     public async Task RemoveFolderMapping_RemovesMapping_WhenFolderExists()
     {
         // Add a folder and a bucket to the database for testing
         await using var dbContext = Fixture.CreateDbContext();
         var folder = new MailFolder { Id = Guid.NewGuid().ToString(), Name = "TestFolder" };
         var bucket = new Bucket { Id = Guid.NewGuid().ToString(), Name = "TestBucket" };
         // Set up the folder with a bucket assignment
         folder.BucketId = bucket.Id;
         dbContext.MailFolders.Add(folder);
         dbContext.Buckets.Add(bucket);
         await dbContext.SaveChangesAsync();
         
         var response = await Client.DeleteAsync($"/settings/folder-mappings/{folder.Name}");
 
         response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
         
         // Verify the mapping was removed from the database
         await using var verifyContext = Fixture.CreateDbContext();
         var dbFolder = await verifyContext.MailFolders.FirstOrDefaultAsync(f => f.Name == folder.Name);
         dbFolder.ShouldNotBeNull();
         dbFolder.BucketId.ShouldBeNull();
     }
     
     [Fact]
     public async Task RemoveFolderMapping_ReturnsNotFound_WhenFolderDoesNotExist()
     {
         var response = await Client.DeleteAsync("/settings/folder-mappings/NonExistentFolder");
 
         response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
     }
}
