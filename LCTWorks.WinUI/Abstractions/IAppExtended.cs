using Microsoft.UI.Xaml;

namespace LCTWorks.WinUI.Abstractions;

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