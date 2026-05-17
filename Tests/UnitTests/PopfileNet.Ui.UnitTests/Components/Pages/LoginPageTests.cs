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

public class LoginPageTests : BunitContext
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly AuthStateProvider _authStateProvider;

    public LoginPageTests()
    {
        JSInterop.SetupFluentUiModules();
        Services.AddSingleton(new LibraryConfiguration());

        _apiClientMock = new Mock<IApiClient>();
        _authStateProvider = new AuthStateProvider(_apiClientMock.Object);
        Services.AddSingleton<IApiClient>(_apiClientMock.Object);
        Services.AddSingleton<AuthenticationStateProvider>(_authStateProvider);
        Services.AddSingleton(_authStateProvider);
    }

    private static string GetErrorMessage(IRenderedComponent<Login> component)
    {
        var prop = component.Instance.GetType().GetProperty("ErrorMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return prop?.GetValue(component.Instance) as string ?? "";
    }

    [Fact]
    public void Login_RendersPageTitle()
    {
        var cut = Render<Login>();

        Assert.Contains("Login", cut.Markup);
    }

    [Fact]
    public async Task Login_SubmitsCredentials_CallsApi()
    {
        _apiClientMock.Setup(m => m.LoginAsync("user@test.com", "password123"))
            .ReturnsAsync(new LoginResponse(true, new UserDto("1", "user@test.com", "Admin"), null));

        var cut = Render<Login>();
        var component = cut.Instance;

        var emailProp = component.GetType().GetProperty("Email",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        emailProp?.SetValue(component, "user@test.com");

        var passwordProp = component.GetType().GetProperty("Password",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        passwordProp?.SetValue(component, "password123");

        await cut.InvokeAsync(async () =>
        {
            var handleMethod = component.GetType().GetMethod("HandleLogin",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handleMethod != null)
            {
                await (Task)handleMethod.Invoke(component, null)!;
            }
        });

        _apiClientMock.Verify(m => m.LoginAsync("user@test.com", "password123"), Times.Once);
    }

    [Fact]
    public async Task Login_ShowsError_OnFailure()
    {
        _apiClientMock.Setup(m => m.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new LoginResponse(false, null, "Invalid credentials"));

        var cut = Render<Login>();
        var component = cut.Instance;

        var emailProp = component.GetType().GetProperty("Email",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        emailProp?.SetValue(component, "user@test.com");

        var passwordProp = component.GetType().GetProperty("Password",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        passwordProp?.SetValue(component, "wrong");

        await cut.InvokeAsync(async () =>
        {
            var handleMethod = component.GetType().GetMethod("HandleLogin",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handleMethod != null)
            {
                await (Task)handleMethod.Invoke(component, null)!;
            }
        });

        GetErrorMessage(cut).ShouldBe("Invalid credentials");
    }

    [Fact]
    public async Task Login_CallsOnLoginSuccess_OnSuccess()
    {
        _apiClientMock.Setup(m => m.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new LoginResponse(true, new UserDto("1", "user@test.com", "Admin"), null));

        var cut = Render<Login>();
        var component = cut.Instance;

        var emailProp = component.GetType().GetProperty("Email",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        emailProp?.SetValue(component, "user@test.com");

        var passwordProp = component.GetType().GetProperty("Password",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        passwordProp?.SetValue(component, "password123");

        await cut.InvokeAsync(async () =>
        {
            var handleMethod = component.GetType().GetMethod("HandleLogin",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handleMethod != null)
            {
                await (Task)handleMethod.Invoke(component, null)!;
            }
        });

        _apiClientMock.Verify(m => m.LoginAsync("user@test.com", "password123"), Times.Once);
        GetErrorMessage(cut).ShouldBe("");
    }

    [Fact]
    public async Task Login_ShowsErrorMessage_OnException()
    {
        _apiClientMock.Setup(m => m.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Network error"));

        var cut = Render<Login>();
        var component = cut.Instance;

        var emailProp = component.GetType().GetProperty("Email",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        emailProp?.SetValue(component, "user@test.com");

        var passwordProp = component.GetType().GetProperty("Password",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        passwordProp?.SetValue(component, "password");

        await cut.InvokeAsync(async () =>
        {
            var handleMethod = component.GetType().GetMethod("HandleLogin",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handleMethod != null)
            {
                await (Task)handleMethod.Invoke(component, null)!;
            }
        });

        GetErrorMessage(cut).ShouldContain("Network error");
    }
}
