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

    private static IJSObjectReference CreateStub() => new StubJsRuntime();

    private class StubJsRuntime : IJSObjectReference
    {
        public ValueTask DisposeAsync() => default;
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