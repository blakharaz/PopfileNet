using Bunit;
using Bunit.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.UnitTests.TestHelpers;
using PopfileNet.Ui.UnitTests.Utils;
using Shouldly;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Components.Pages;

public class SettingsPageTests : BunitContext
{
    public SettingsPageTests()
    {
        JSInterop.SetupFluentUiModules();
        
        Services.AddSingleton(new LibraryConfiguration());
        Services.AddSingleton<IApiClient>(new MockApiClient());
    }
    
    [Fact]
    public async Task Settings_RendersPageTitle()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsImapServerField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Server", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsPortField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Port", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsUsernameField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Username", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsPasswordField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Password", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsSaveButton()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Save", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsTestConnectionButton()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Test Connection", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsBucketsSection()
    {
        var cut = Render<Settings>();
        
        // Wait for async initialization
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Buckets", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsFolderMappingsSection()
    {
        var cut = Render<Settings>();
        
        // Wait for async initialization
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Folder Mappings", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public void Settings_ContainsAddBucketButton()
    {
        var cut = Render<Settings>();
        
        Assert.Contains("Add Bucket", cut.Markup);
    }
    
    [Fact]
    public async Task Settings_ContainsUseSslSwitch()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Use SSL", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ContainsMaxParallelConnectionsField()
    {
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Max Parallel Connections", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public void Settings_HasCorrectImapCard()
    {
        var cut = Render<Settings>();
        
        Assert.NotNull(cut.Find("[data-testid='settings-imap-card']"));
    }
    
    [Fact]
    public void Settings_HasCorrectBucketsCard()
    {
        var cut = Render<Settings>();
        
        Assert.NotNull(cut.Find("[data-testid='settings-buckets-card']"));
    }
    
    [Fact]
    public async Task Settings_ShowsStatusMessage_WhenStatusMessageIsSet()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.StatusMessage = "Test status message";
        cut.Render();
        
        Assert.Contains("Test status message", cut.Markup);
        Assert.NotNull(cut.Find("[data-testid='status-message-card']"));
        Assert.NotNull(cut.Find("[data-testid='status-badge']"));
    }
    
    [Fact]
    public async Task Settings_DoesNotShowStatusMessage_WhenStatusMessageIsEmpty()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.DoesNotContain("data-testid=\"status-message-card\"", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ShowsEditMappingForm_WhenEditMappingNameIsSet()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
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
        
        cut.Render();
        
        Assert.NotNull(cut.Find("[data-testid='edit-mapping-form']"));
        Assert.NotNull(cut.Find("[data-testid='edit-folder-name']"));
        Assert.NotNull(cut.Find("[data-testid='edit-bucket-dropdown']"));
        Assert.NotNull(cut.Find("[data-testid='save-mapping']"));
        Assert.NotNull(cut.Find("[data-testid='cancel-mapping']"));
    }
    
    [Fact]
    public async Task Settings_DoesNotShowEditMappingForm_WhenEditMappingNameIsNull()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.DoesNotContain("data-testid=\"edit-mapping-form\"", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ShowsBucketDropdownOptions_WhenBucketsExist()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
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
        
        cut.Render();
        
        var dropdown = cut.Find("[data-testid='edit-bucket-dropdown']");
        Assert.Contains("(None)", dropdown.InnerHtml);
        Assert.Contains("Work", dropdown.InnerHtml);
        Assert.Contains("Personal", dropdown.InnerHtml);
    }
    
    [Fact]
    public async Task Settings_ShowsFolderNameInEditForm_WhenEditMappingStarted()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
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
        
        cut.Render();
        
        Assert.Contains("Inbox", cut.Markup);
    }
    
    [Fact]
    public async Task Settings_ShowsFolderMappingsTable_WithCorrectHeaders()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Folder", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        Assert.Contains("Assigned Bucket", cut.Markup);
        Assert.Contains("Actions", cut.Markup);
    }
    
    [Fact]
    public async Task Settings_ShowsBucketsTable_WithCorrectHeaders()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Name", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        Assert.Contains("Description", cut.Markup);
        Assert.Contains("Actions", cut.Markup);
    }
    
    [Fact]
    public async Task Settings_ShowsFolderMappingRow_WithCorrectData()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Inbox", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        Assert.Contains("Work", cut.Markup);
        Assert.NotNull(cut.Find("[data-testid='folder-name']"));
        Assert.NotNull(cut.Find("[data-testid='folder-bucket']"));
    }
    
    [Fact]
    public async Task Settings_ShowsEditAndRemoveButtons_ForEachFolderMapping()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Inbox", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var markup = cut.Markup;
        markup.ShouldContain("edit-mapping-btn");
        markup.ShouldContain("remove-mapping-btn");
    }
    
    [Fact]
    public async Task Settings_ShowsSaveAndDeleteButtons_ForEachBucket()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("Work", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var markup = cut.Markup;
        // Count Save buttons in the buckets table
        var saveButtonCount = markup.Split("Save").Length - 1;
        var deleteButtonCount = markup.Split("Delete").Length - 1;
        
        saveButtonCount.ShouldBeGreaterThanOrEqualTo(2); // At least 2 buckets
        deleteButtonCount.ShouldBeGreaterThanOrEqualTo(2);
    }
    
    [Fact]
    public async Task Settings_ImapCard_HasCorrectTestId()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.NotNull(cut.Find("[data-testid='settings-imap-card']")),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_ImapStack_HasCorrectTestId()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.NotNull(cut.Find("[data-testid='settings-imap-stack']")),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_NoFolderMappings_HasCorrectTestId()
    {
        Services.AddSingleton<IApiClient>(new MockApiClient());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.NotNull(cut.Find("[data-testid='no-folder-mappings']")),
            TimeSpan.FromSeconds(2)));
    }
    
    [Fact]
    public async Task Settings_OnInitialized_HandlesNullSettings()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithNullSettings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.Server.ShouldBe("");
        component.Port.ShouldBe(993);
    }
    
    [Fact]
    public async Task Settings_OnInitialized_HandlesNullImapSettings()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithNullImapSettings());
        
        var cut = Render<Settings>();
        
        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("IMAP Settings", cut.Markup),
            TimeSpan.FromSeconds(2)));
        
        var component = cut.Instance;
        component.Server.ShouldBe("");
        component.Port.ShouldBe(993);
        component.UseSsl.ShouldBeTrue();
    }
    
    [Fact]
    public async Task Settings_StartEditMapping_SetsSelectedBucketId()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithBucketsAndMappings());
        
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
        
        var selectedBucketIdField = component.GetType().GetField("_editSelectedBucketId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var selectedBucketId = selectedBucketIdField?.GetValue(component) as string;
        
        selectedBucketId.ShouldBe("bucket-1");
    }
    
    [Fact]
    public async Task Settings_SaveMapping_RefreshesFolderMappings()
    {
        var mockApi = new MockApiClientWithRefreshTracking();
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
        
        mockApi.GetSettingsCallCount.ShouldBeGreaterThanOrEqualTo(2);
    }
    
    [Fact]
    public async Task Settings_RemoveMapping_RefreshesFolderMappings()
    {
        var mockApi = new MockApiClientWithRefreshTracking();
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
        
        mockApi.GetSettingsCallCount.ShouldBeGreaterThanOrEqualTo(2);
    }
    
    [Fact]
    public async Task Settings_SaveBucket_SetsId_OnNewBucketCreation()
    {
        var mockApi = new MockApiClientWithCreatedBucket();
        Services.AddSingleton<IApiClient>(mockApi);
        
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
        
        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveBucket", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, [newBucket])!;
            }
        });
        
        newBucket.Id.ShouldBe("new-bucket-id");
    }
    
    [Fact]
    public async Task Settings_SaveBucket_HandlesNullApiResponse()
    {
        Services.AddSingleton<IApiClient>(new MockApiClientWithNullCreateResponse());
        
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
        newBucket.Id.ShouldBe(""); // Should remain empty since API returned null
    }
    
    [Fact]
    public async Task Settings_TestConnection_HandlesException()
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
            var testMethod = component.GetType().GetMethod("TestConnection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (testMethod != null)
            {
                await (Task)testMethod.Invoke(component, null)!;
            }
        });
        
        component.StatusMessage.ShouldContain("Error:");
    }
}

public class MockApiClientWithNullSettings : MockApiClient
{
    public override Task<AppSettingsDto?> GetSettingsAsync() =>
        Task.FromResult<AppSettingsDto?>(null);
}

public class MockApiClientWithNullImapSettings : MockApiClient
{
    public override Task<AppSettingsDto?> GetSettingsAsync() =>
        Task.FromResult<AppSettingsDto?>(new AppSettingsDto
        {
            ImapSettings = null,
            Buckets = [],
            FolderMappings = []
        });
}

public class MockApiClientWithRefreshTracking : MockApiClient
{
    public int GetSettingsCallCount { get; private set; }
    
    public override Task<AppSettingsDto?> GetSettingsAsync()
    {
        GetSettingsCallCount++;
        return Task.FromResult<AppSettingsDto?>(new AppSettingsDto
        {
            ImapSettings = new ImapSettingsDto(),
            Buckets = [],
            FolderMappings = [new FolderMappingDto("TestFolder", "bucket-1")]
        });
    }
}

public class MockApiClientWithCreatedBucket : MockApiClient
{
    public override Task<BucketDto?> CreateBucketAsync(BucketDto bucket) =>
        Task.FromResult<BucketDto?>(new BucketDto("new-bucket-id", bucket.Name, bucket.Description));
}

public class MockApiClientWithNullCreateResponse : MockApiClient
{
    public override Task<BucketDto?> CreateBucketAsync(BucketDto bucket) =>
        Task.FromResult<BucketDto?>(null);
}
