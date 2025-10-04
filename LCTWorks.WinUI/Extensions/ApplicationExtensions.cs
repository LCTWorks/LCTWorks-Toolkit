using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Extensions;

public static class ApplicationExtensions
{
    public static IAppExtended? AsAppExtended(this Application app)
    {
        if (app is IAppExtended appExtended)
        {
            return appExtended;
        }
        return default;
    }

    public static Frame? GetContentAsFrame(this Application app)
        => app.GetMainWindow()?.Content as Frame;

    public static Window? GetMainWindow(this Application app)
            => app.AsAppExtended()?.MainWindow;

    public static XamlRoot? GetXamlRoot(this Application app)
            => app.GetMainWindow()?.Content?.XamlRoot;
}