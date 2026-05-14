using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.JSInterop;

namespace PopfileNet.Ui.UnitTests.Utils;

public static class FluentUiSetupExtensions
{
    public static void SetupFluentUiModules(this BunitJSInterop jsInterop)
    {
        var fluentAssembly = typeof(Microsoft.FluentUI.AspNetCore.Components.FluentComponentBase).Assembly;
        var fluentVersion = fluentAssembly.GetName().Version?.ToString() ?? "4.14.1.26112";

        var modulePath = $"./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Label/FluentInputLabel.razor.js?v={fluentVersion}";

        jsInterop.SetupModule(modulePath).SetupVoid("setInputAriaLabel", _ => true);

        var textFieldModulePath = $"./_content/Microsoft.FluentUI.AspNetCore.Components/Components/TextField/FluentTextField.razor.js?v={fluentVersion}";
        jsInterop.SetupModule(textFieldModulePath).SetupVoid("ensureCurrentValueMatch", _ => true);
    }

    /// <summary>
    /// Creates a stub JS interop reference for module setup.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static IJSObjectReference CreateStub() => new StubJsRuntime();

    /// <summary>
    /// Stub implementation of IJSObjectReference used by the test helpers.
    /// Excluded from code coverage: this is a simple stub for bunit's JS interop mocking.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class StubJsRuntime : IJSObjectReference
    {
        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return default;
        }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            throw new NotImplementedException();
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            throw new NotImplementedException();
        }
    }
}