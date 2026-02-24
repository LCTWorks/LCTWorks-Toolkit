using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Experimental.Helpers;

public static class TitleBarHelperExperimental
{
    public static void Extend(TitleBar titleBar, NavigationView navigationView, FrameNavigationService navigationService, IAppExtended? app = null)
    {
        if (titleBar == null)
        {
            return;
        }
        titleBar.BackRequested += (sender, args) =>
        {
            if (navigationService.CanGoBack)
            {
                navigationService.GoBack();
            }
        };
        if (navigationView != null)
        {
            navigationView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
            navigationView.IsPaneToggleButtonVisible = false;

            titleBar.PaneToggleRequested += (sender, args) =>
            {
                navigationView.IsPaneOpen = !navigationView.IsPaneOpen;
            };
        }

        app ??= Application.Current.AsAppExtended();
        if (app == null)
        {
            return;
        }
        app.MainWindow.ExtendsContentIntoTitleBar = true;
        app.MainWindow.SetTitleBar(titleBar);
    }
}