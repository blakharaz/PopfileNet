using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using PopfileNet.Ui.Components.Pages;
using PopfileNet.Ui.Services;
using PopfileNet.Ui.UnitTests.Utils;
using Shouldly;
using Xunit;

namespace PopfileNet.Ui.UnitTests.Components.Pages;

public class UsersPageTests : BunitContext
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly AuthStateProvider _authStateProvider;

    public UsersPageTests()
    {
        JSInterop.SetupFluentUiModules();
        Services.AddSingleton(new LibraryConfiguration());

        _apiClientMock = new Mock<IApiClient>();
        _authStateProvider = new AuthStateProvider(_apiClientMock.Object);
        Services.AddSingleton<IApiClient>(_apiClientMock.Object);
        Services.AddSingleton<AuthenticationStateProvider>(_authStateProvider);
        Services.AddSingleton(_authStateProvider);
    }

    private static string GetStatusMessage(IRenderedComponent<Users> component)
    {
        var prop = component.Instance.GetType().GetProperty("StatusMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return prop?.GetValue(component.Instance) as string ?? "";
    }

    [Fact]
    public async Task Users_RendersPageTitle()
    {
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResponse<UserDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true });

        var cut = Render<Users>();

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("User Management", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task Users_ShowsNoUsers_WhenEmpty()
    {
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResponse<UserDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true });

        var cut = Render<Users>();

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("No users found", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task Users_DisplaysUsers_WhenUsersExist()
    {
        var users = new PagedResponse<UserDto>
        {
            Items = [new UserDto("1", "admin@test.com", "Admin")],
            Page = 1, PageSize = 20, TotalCount = 1, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true
        };
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(users);

        var cut = Render<Users>();

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("admin@test.com", cut.Markup),
            TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task Users_AddUser_ShowsEditForm()
    {
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResponse<UserDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true });

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("User Management", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(() =>
        {
            var addMethod = component.GetType().GetMethod("ShowAddUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            addMethod?.Invoke(component, null);
        });

        var isEditingField = component.GetType().GetField("_isEditing",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.True((bool)(isEditingField?.GetValue(component) ?? false));
    }

    [Fact]
    public async Task Users_SaveUser_CreatesNewUser()
    {
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResponse<UserDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true });
        _apiClientMock.Setup(m => m.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new UserDto("new-id", "new@test.com", "User"));

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("User Management", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(() =>
        {
            var addMethod = component.GetType().GetMethod("ShowAddUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            addMethod?.Invoke(component, null);
        });

        var editEmailField = component.GetType().GetField("_editEmail",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        editEmailField?.SetValue(component, "new@test.com");

        var editPasswordField = component.GetType().GetField("_editPassword",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        editPasswordField?.SetValue(component, "password");

        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });

        _apiClientMock.Verify(m => m.CreateUserAsync("new@test.com", "password", "User"), Times.Once);
    }

    [Fact]
    public async Task Users_SaveUser_ShowsSuccessMessage()
    {
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResponse<UserDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true });
        _apiClientMock.Setup(m => m.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new UserDto("new-id", "new@test.com", "User"));

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("User Management", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(() =>
        {
            var addMethod = component.GetType().GetMethod("ShowAddUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            addMethod?.Invoke(component, null);
        });

        var editEmailField = component.GetType().GetField("_editEmail",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        editEmailField?.SetValue(component, "new@test.com");

        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });

        GetStatusMessage(cut).ShouldContain("User created successfully");
    }

    [Fact]
    public async Task Users_DeleteUser_CallsApi()
    {
        var users = new PagedResponse<UserDto>
        {
            Items = [new UserDto("1", "user@test.com", "User")],
            Page = 1, PageSize = 20, TotalCount = 1, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true
        };
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(users);
        _apiClientMock.Setup(m => m.DeleteUserAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("user@test.com", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(async () =>
        {
            var deleteMethod = component.GetType().GetMethod("DeleteUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (deleteMethod != null)
            {
                await (Task)deleteMethod.Invoke(component, ["1"])!;
            }
        });

        _apiClientMock.Verify(m => m.DeleteUserAsync("1"), Times.Once);
    }

    [Fact]
    public async Task Users_DeleteUser_ShowsSuccessMessage()
    {
        var users = new PagedResponse<UserDto>
        {
            Items = [new UserDto("1", "user@test.com", "User")],
            Page = 1, PageSize = 20, TotalCount = 1, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true
        };
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(users);
        _apiClientMock.Setup(m => m.DeleteUserAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("user@test.com", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(async () =>
        {
            var deleteMethod = component.GetType().GetMethod("DeleteUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (deleteMethod != null)
            {
                await (Task)deleteMethod.Invoke(component, ["1"])!;
            }
        });

        GetStatusMessage(cut).ShouldContain("User deleted successfully");
    }

    [Fact]
    public async Task Users_CancelEdit_ClearsEditState()
    {
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResponse<UserDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true });

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("User Management", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(() =>
        {
            var addMethod = component.GetType().GetMethod("ShowAddUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            addMethod?.Invoke(component, null);
        });

        await cut.InvokeAsync(() =>
        {
            var cancelMethod = component.GetType().GetMethod("CancelEdit",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cancelMethod?.Invoke(component, null);
        });

        var isEditingField = component.GetType().GetField("_isEditing",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.False((bool)(isEditingField?.GetValue(component) ?? false));
    }

    [Fact]
    public async Task Users_SaveUser_ShowsError_OnFailure()
    {
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResponse<UserDto> { Items = [], Page = 1, PageSize = 20, TotalCount = 0, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true });
        _apiClientMock.Setup(m => m.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("API Error"));

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("User Management", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(() =>
        {
            var addMethod = component.GetType().GetMethod("ShowAddUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            addMethod?.Invoke(component, null);
        });

        var editEmailField = component.GetType().GetField("_editEmail",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        editEmailField?.SetValue(component, "new@test.com");

        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });

        GetStatusMessage(cut).ShouldContain("Error:");
    }

    [Fact]
    public async Task Users_DeleteUser_ShowsError_OnFailure()
    {
        var users = new PagedResponse<UserDto>
        {
            Items = [new UserDto("1", "user@test.com", "User")],
            Page = 1, PageSize = 20, TotalCount = 1, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true
        };
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(users);
        _apiClientMock.Setup(m => m.DeleteUserAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Delete failed"));

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("user@test.com", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(async () =>
        {
            var deleteMethod = component.GetType().GetMethod("DeleteUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (deleteMethod != null)
            {
                await (Task)deleteMethod.Invoke(component, ["1"])!;
            }
        });

        GetStatusMessage(cut).ShouldContain("Error:");
    }

    [Fact]
    public async Task Users_StartEditUser_SetsEditState()
    {
        var users = new PagedResponse<UserDto>
        {
            Items = [new UserDto("1", "user@test.com", "User")],
            Page = 1, PageSize = 20, TotalCount = 1, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true
        };
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(users);

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("user@test.com", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(() =>
        {
            var startMethod = component.GetType().GetMethod("StartEditUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(component, [new UserDto("1", "user@test.com", "User")]);
        });

        var isEditingField = component.GetType().GetField("_isEditing",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.True((bool)(isEditingField?.GetValue(component) ?? false));

        var editEmailField = component.GetType().GetField("_editEmail",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.Equal("user@test.com", editEmailField?.GetValue(component));
    }

    [Fact]
    public async Task Users_UpdateUser_CallsApi()
    {
        var users = new PagedResponse<UserDto>
        {
            Items = [new UserDto("1", "user@test.com", "User")],
            Page = 1, PageSize = 20, TotalCount = 1, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true
        };
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(users);
        _apiClientMock.Setup(m => m.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new UserDto("1", "updated@test.com", "Admin"));

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("user@test.com", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(() =>
        {
            var startMethod = component.GetType().GetMethod("StartEditUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(component, [new UserDto("1", "user@test.com", "User")]);
        });

        var editEmailField = component.GetType().GetField("_editEmail",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        editEmailField?.SetValue(component, "updated@test.com");

        var editRoleField = component.GetType().GetField("_editRole",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        editRoleField?.SetValue(component, "Admin");

        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });

        _apiClientMock.Verify(m => m.UpdateUserAsync("1", "updated@test.com", "Admin"), Times.Once);
    }

    [Fact]
    public async Task Users_UpdateUser_ShowsSuccessMessage()
    {
        var users = new PagedResponse<UserDto>
        {
            Items = [new UserDto("1", "user@test.com", "User")],
            Page = 1, PageSize = 20, TotalCount = 1, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true
        };
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(users);
        _apiClientMock.Setup(m => m.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new UserDto("1", "updated@test.com", "Admin"));

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("user@test.com", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(() =>
        {
            var startMethod = component.GetType().GetMethod("StartEditUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(component, [new UserDto("1", "user@test.com", "User")]);
        });

        var editEmailField = component.GetType().GetField("_editEmail",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        editEmailField?.SetValue(component, "updated@test.com");

        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });

        GetStatusMessage(cut).ShouldContain("User updated successfully");
    }

    [Fact]
    public async Task Users_UpdateUser_ShowsError_OnFailure()
    {
        var users = new PagedResponse<UserDto>
        {
            Items = [new UserDto("1", "user@test.com", "User")],
            Page = 1, PageSize = 20, TotalCount = 1, TotalPages = 1, HasPrevious = false, HasNext = false, IsSuccess = true
        };
        _apiClientMock.Setup(m => m.GetUsersAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(users);
        _apiClientMock.Setup(m => m.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("Update failed"));

        var cut = Render<Users>();
        var component = cut.Instance;

        await cut.InvokeAsync(() => cut.WaitForAssertion(
            () => Assert.Contains("user@test.com", cut.Markup),
            TimeSpan.FromSeconds(2)));

        await cut.InvokeAsync(() =>
        {
            var startMethod = component.GetType().GetMethod("StartEditUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(component, [new UserDto("1", "user@test.com", "User")]);
        });

        await cut.InvokeAsync(async () =>
        {
            var saveMethod = component.GetType().GetMethod("SaveUser",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (saveMethod != null)
            {
                await (Task)saveMethod.Invoke(component, null)!;
            }
        });

        GetStatusMessage(cut).ShouldContain("Error:");
    }
}
