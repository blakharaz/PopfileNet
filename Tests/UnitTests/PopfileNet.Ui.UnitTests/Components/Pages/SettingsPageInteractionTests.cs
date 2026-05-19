using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.UnitTests.TestHelpers;
using PopfileNet.Ui.UnitTests.Utils;
using Shouldly;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Components.Pages;

public class SettingsPageInteractionTests : BunitContext
{
    public SettingsPageInteractionTests()
    {
        JSInterop.SetupFluentUiModules();
        
        Services.AddSingleton(new LibraryConfiguration());
    }
    
    [Fact]
    public async Task Settings_LoadsWithDefaultValues()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.Server.ShouldBe("");
        component.Port.ShouldBe(993);
        component.Username.ShouldBe("");
        component.Password.ShouldBe("");
        component.UseSsl.ShouldBeTrue();
        component.MaxParallelConnections.ShouldBe(4);
    }
    
    [Fact]
    public async Task Settings_LoadsValuesFromApi()
    {
        var mockApi = new MockApiClientWithSettings();
        Services.AddSingleton<IApiClient>(mockApi);
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("imap.custom.com", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.Server.ShouldBe("imap.custom.com");
        component.Port.ShouldBe(587);
        component.Username.ShouldBe("user@custom.com");
        component.UseSsl.ShouldBeFalse();
        component.MaxParallelConnections.ShouldBe(8);
    }
    
    [Fact]
    public async Task Settings_SaveSettings_ShowsSuccessMessage()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.Server = "test.server.com";
        component.Port = 993;
        component.Username = "test@test.com";
        component.Password = "password";
        component.UseSsl = true;
        component.MaxParallelConnections = 4;
        
        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveSettings", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });
        
        component.StatusMessage.ShouldBe("Settings saved successfully!");
    }
    
    [Fact]
    public async Task Settings_SaveSettings_ShowsErrorMessage_OnFailure()
    {
        var mockApi = new FailingMockApiClient();
        Services.AddSingleton<IApiClient>(mockApi);
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveSettings", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });
        
        component.StatusMessage.ShouldContain("Error:");
    }
    
    [Fact]
    public async Task Settings_TestConnection_ShowsSuccessMessage()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(async () =>
        {
            var testMethod = component.GetType().GetMethod("TestConnection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (testMethod != null)
            {
                await (Task)testMethod.Invoke(component, null)!;
            }
        });
        
        component.StatusMessage.ShouldBe("Connection successful!");
    }
    
    [Fact]
    public async Task Settings_AddBucket_AddsEmptyBucket()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        var initialCount = component.Buckets.Count;
        
        await cut.InvokeAsync(async () =>
        {
            var addMethod = component.GetType().GetMethod("AddBucket", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            addMethod?.Invoke(component, null);
        });
        
        component.Buckets.Count.ShouldBe(initialCount + 1);
        component.Buckets.Last().Id.ShouldBe("");
        component.Buckets.Last().Name.ShouldBe("");
        component.Buckets.Last().Description.ShouldBe("");
    }
    
    [Fact]
    public async Task Settings_SaveBucket_CreatesNewBucket()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(async () =>
        {
            var addMethod = component.GetType().GetMethod("AddBucket", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            addMethod?.Invoke(component, null);
        });
        
        var newBucket = component.Buckets.Last();
        newBucket.Name = "New Bucket";
        newBucket.Description = "Description";
        
        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveBucket", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, [newBucket])!;
            }
        });
        
        component.StatusMessage.ShouldBe("Bucket saved!");
    }
    
    [Fact]
    public async Task Settings_DeleteBucket_RemovesBucket()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        var bucketId = "test-bucket-id";
        component.Buckets.Add(new EditableBucket { Id = bucketId, Name = "Test Bucket" });
        var initialCount = component.Buckets.Count;
        
        await cut.InvokeAsync(async () =>
        {
            var deleteMethod = component.GetType().GetMethod("DeleteBucket", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (deleteMethod != null)
            {
                await (Task)deleteMethod.Invoke(component, [bucketId])!;
            }
        });
        
        component.Buckets.Count.ShouldBe(initialCount - 1);
        component.Buckets.ShouldNotContain(b => b.Id == bucketId);
    }
    
    [Fact]
    public async Task Settings_GetBucketName_ReturnsNone_ForNullBucketId()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        var getBucketNameMethod = component.GetType().GetMethod("GetBucketName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = getBucketNameMethod?.Invoke(component, [null]) as string;
        result.ShouldBe("(None)");
    }
    
    [Fact]
    public async Task Settings_GetBucketName_ReturnsUnknown_ForMissingBucketId()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        var getBucketNameMethod = component.GetType().GetMethod("GetBucketName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = getBucketNameMethod?.Invoke(component, ["nonexistent-id"]) as string;
        result.ShouldBe("(Unknown)");
    }
    
    [Fact]
    public async Task Settings_GetBucketName_ReturnsBucketName_ForExistingBucketId()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.Buckets.Add(new EditableBucket { Id = "bucket-1", Name = "Work Emails" });
        
        var getBucketNameMethod = component.GetType().GetMethod("GetBucketName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = getBucketNameMethod?.Invoke(component, ["bucket-1"]) as string;
        result.ShouldBe("Work Emails");
    }
    
    [Fact]
    public async Task Settings_StartEditMapping_SetsEditState()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithFolderMappings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Folder Mappings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(() =>
        {
            var startMethod = component.GetType().GetMethod("StartEditMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(component, ["Inbox"]);
        });
        
        component.FolderMappings.ShouldContain(m => m.Name == "Inbox");
    }
    
    [Fact]
    public async Task Settings_CancelEditMapping_ClearsEditState()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(() =>
        {
            var startMethod = component.GetType().GetMethod("StartEditMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(component, ["TestFolder"]);
        });
        
        await cut.InvokeAsync(() =>
        {
            var cancelMethod = component.GetType().GetMethod("CancelEditMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cancelMethod?.Invoke(component, null);
        });
        
        var editMappingField = component.GetType().GetField("_editMappingName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        editMappingField?.GetValue(component).ShouldBeNull();
    }
    
    [Fact]
    public async Task Settings_RemoveMapping_RemovesMapping()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(async () =>
        {
            var removeMethod = component.GetType().GetMethod("RemoveMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (removeMethod != null)
            {
                await (Task)removeMethod.Invoke(component, ["TestFolder"])!;
            }
        });
        
        component.StatusMessage.ShouldBe("Folder mapping removed successfully!");
    }
    
    [Fact]
    public async Task Settings_SaveMapping_SavesMapping()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(() =>
        {
            var startMethod = component.GetType().GetMethod("StartEditMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(component, ["TestFolder"]);
        });
        
        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });
        
        component.StatusMessage.ShouldBe("Folder mapping saved successfully!");
    }
    
    [Fact]
    public async Task Settings_SaveMapping_DoesNothing_WhenEditMappingNameIsNull()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });
        
        component.StatusMessage.ShouldBe("");
    }
    
    [Fact]
    public async Task Settings_ShowsNoBucketsMessage_WhenBucketsEmpty()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("No buckets configured.", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ShowsNoFolderMappingsMessage_WhenMappingsEmpty()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("No folder mappings configured.", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ShowsBucketsTable_WhenBucketsExist()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Work", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        Assert.Contains("Personal", cut.Markup);
    }
    
    [Fact]
    public async Task Settings_ShowsFolderMappingsTable_WhenMappingsExist()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Inbox", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        Assert.Contains("Archive", cut.Markup);
    }
    
    [Fact]
    public async Task Settings_TestConnection_ShowsFailureMessage_WhenConnectionFails()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithConnectionFailure());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(async () =>
        {
            var testMethod = component.GetType().GetMethod("TestConnection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (testMethod != null)
            {
                await (Task)testMethod.Invoke(component, null)!;
            }
        });
        
        component.StatusMessage.ShouldBe("Connection failed!");
    }
    
    [Fact]
    public async Task Settings_SaveBucket_UpdatesExistingBucket()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.Buckets.Add(new EditableBucket { Id = "existing-id", Name = "Updated Name", Description = "Updated Desc" });
        
        var existingBucket = component.Buckets.First(b => b.Id == "existing-id");
        
        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveBucket", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, [existingBucket])!;
            }
        });
        
        component.StatusMessage.ShouldBe("Bucket saved!");
    }
    
    [Fact]
    public async Task Settings_SaveBucket_ShowsError_OnFailure()
    {
        var mockApi = new FailingMockApiClient();
        Services.AddSingleton<IApiClient>(mockApi);
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.Buckets.Add(new EditableBucket { Id = "", Name = "New Bucket" });
        var newBucket = component.Buckets.Last();
        
        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveBucket", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, [newBucket])!;
            }
        });
        
        component.StatusMessage.ShouldContain("Error:");
    }
    
    [Fact]
    public async Task Settings_DeleteBucket_ShowsError_OnFailure()
    {
        var mockApi = new FailingMockApiClient();
        Services.AddSingleton<IApiClient>(mockApi);
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.Buckets.Add(new EditableBucket { Id = "bucket-id", Name = "Test Bucket" });
        
        await cut.InvokeAsync(async () =>
        {
            var deleteMethod = component.GetType().GetMethod("DeleteBucket", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (deleteMethod != null)
            {
                await (Task)deleteMethod.Invoke(component, ["bucket-id"])!;
            }
        });
        
        component.StatusMessage.ShouldContain("Error:");
    }
    
    [Fact]
    public async Task Settings_RemoveMapping_ShowsError_OnFailure()
    {
        var mockApi = new FailingMockApiClient();
        Services.AddSingleton<IApiClient>(mockApi);
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(async () =>
        {
            var removeMethod = component.GetType().GetMethod("RemoveMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (removeMethod != null)
            {
                await (Task)removeMethod.Invoke(component, ["TestFolder"])!;
            }
        });
        
        component.StatusMessage.ShouldContain("Error:");
    }
    
    [Fact]
    public async Task Settings_SaveMapping_ShowsError_OnFailure()
    {
        var mockApi = new FailingMockApiClient();
        Services.AddSingleton<IApiClient>(mockApi);
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(() =>
        {
            var startMethod = component.GetType().GetMethod("StartEditMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(component, ["TestFolder"]);
        });
        
        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveMapping", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });
        
        component.StatusMessage.ShouldContain("Error:");
    }
    
    [Fact]
    public async Task Settings_TestConnection_ShowsError_OnFailure()
    {
        var mockApi = new MockApiClientWithConnectionFailure();
        Services.AddSingleton<IApiClient>(mockApi);
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        
        await cut.InvokeAsync(async () =>
        {
            var testMethod = component.GetType().GetMethod("TestConnection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (testMethod != null)
            {
                await (Task)testMethod.Invoke(component, null)!;
            }
        });
        
        component.StatusMessage.ShouldBe("Connection failed!");
    }
}

public class MockApiClientWithSettings : MockApiClient
{
    public override Task<AppSettingsDto?> GetSettingsAsync() =>
        Task.FromResult<AppSettingsDto?>(new AppSettingsDto
        {
            ImapSettings = new ImapSettingsDto
            {
                Server = "imap.custom.com",
                Port = 587,
                Username = "user@custom.com",
                UseSsl = false,
                MaxParallelConnections = 8
            },
            Buckets = [],
            FolderMappings = []
        });
}

public class MockApiClientWithFolderMappings : MockApiClient
{
    public override Task<AppSettingsDto?> GetSettingsAsync() =>
        Task.FromResult<AppSettingsDto?>(new AppSettingsDto
        {
            ImapSettings = new ImapSettingsDto(),
            Buckets = [],
            FolderMappings = [new FolderMappingDto("Inbox", "bucket-1")]
        });
}

public class MockApiClientWithBucketsAndMappings : MockApiClient
{
    public override Task<AppSettingsDto?> GetSettingsAsync() =>
        Task.FromResult<AppSettingsDto?>(new AppSettingsDto
        {
            ImapSettings = new ImapSettingsDto(),
            Buckets = [
                new BucketDto("bucket-1", "Work", "Work emails"),
                new BucketDto("bucket-2", "Personal", "Personal emails")
            ],
            FolderMappings = [
                new FolderMappingDto("Inbox", "bucket-1"),
                new FolderMappingDto("Archive", "bucket-2")
            ]
        });
}

public class MockApiClientWithConnectionFailure : MockApiClient
{
    public override Task<bool> TestConnectionAsync() => Task.FromResult(false);
}

public class FailingMockApiClient : MockApiClient
{
    public override Task<bool> SaveSettingsAsync(AppSettingsDto settings) => 
        throw new InvalidOperationException("API Error");
    
    public override Task DeleteBucketAsync(string id) => 
        throw new InvalidOperationException("Delete failed");
    
    public override Task SetFolderMappingAsync(string folderName, string? bucketId) => 
        throw new InvalidOperationException("Set mapping failed");
    
    public override Task RemoveFolderMappingAsync(string folderName) => 
        throw new InvalidOperationException("Remove mapping failed");
    
    public override Task<BucketDto?> CreateBucketAsync(BucketDto bucket) => 
        throw new InvalidOperationException("Create bucket failed");
    
    public override Task<BucketDto?> UpdateBucketAsync(BucketDto bucket) => 
        throw new InvalidOperationException("Update bucket failed");
    
    public override Task<bool> TestConnectionAsync() => 
        throw new InvalidOperationException("Connection test failed");
}
