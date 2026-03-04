using Bunit;

namespace PopfileNet.Ui.Tests.Utils;

public static class FluentUiSetupExtensions
{
    public static void SetupFluentUiModules(this BunitJSInterop jsInterop)
    {
        const string fluentVersion = "4.14.0.26043";
        
        var inputLabel = jsInterop.SetupModule(
            $"./_content/Microsoft.FluentUI.AspNetCore.Components/Components/Label/FluentInputLabel.razor.js?v={fluentVersion}");
        inputLabel.SetupVoid("setInputAriaLabel", _ => true);

        var textField = jsInterop.SetupModule(
            $"./_content/Microsoft.FluentUI.AspNetCore.Components/Components/TextField/FluentTextField.razor.js?v={fluentVersion}");
        textField.SetupVoid("ensureCurrentValueMatch", _ => true);
    }
}