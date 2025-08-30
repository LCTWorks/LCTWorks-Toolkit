using Microsoft.UI.Xaml;

namespace LCTWorks.Common.WinUI.Abstractions;

public interface IAppExtended
{
    UIElement? AppTitleBar
    {
        get; set;
    }

    Window MainWindow
    {
        get;
    }
}