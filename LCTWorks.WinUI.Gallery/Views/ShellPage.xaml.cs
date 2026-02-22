using CommunityToolkit.WinUI;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Gallery.ViewModels;
using LCTWorks.WinUI.Helpers;
using LCTWorks.WinUI.Navigation;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using Windows.Graphics;

namespace LCTWorks.WinUI.Gallery.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage(
        ShellViewModel viewModel,
        FrameNavigationService navigationService)
    {
        ViewModel = viewModel;
        InitializeComponent();

        navigationService.Frame = NavigationFrame;
        NavigationViewHelper.Configure(navigationService, NavigationViewControl, typeof(SettingsViewModel).FullName!);

        TitleBarHelper.Extend(AppTitleBar, AppTitleBarText, "AppDisplayName".GetTextLocalized());
    }

    public ShellViewModel ViewModel { get; private set; }

    private void AppTitleBar_Loaded(object _, RoutedEventArgs __)
    {
        SetRegionsForCustomTitleBar();
        TitleBarHelper.UpdateTitleBar(RequestedTheme);
    }

    private void AppTitleBar_SizeChanged(object _, SizeChangedEventArgs __)
                => SetRegionsForCustomTitleBar();

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs __)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }

    private void NavigationViewControl_Loaded(object sender, RoutedEventArgs __)
    {
        var settings = ((NavigationView)sender).SettingsItem as NavigationViewItem;
        settings?.FontFamily = new FontFamily("ms-appx:///Assets/Fonts/Poppins-Regular.ttf#Poppins");
    }

    private void SetRegionsForCustomTitleBar()
    {
        var m_AppWindow = App.MainWindow.AppWindow;
        var scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;

        RightPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.RightInset / scaleAdjustment);
        LeftPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.LeftInset / scaleAdjustment);

        //var transform = TitleBarSearchBox.TransformToVisual(null);
        //var bounds = transform.TransformBounds(new Rect(0, 0, TitleBarSearchBox.ActualWidth, TitleBarSearchBox.ActualHeight));
        //var SearchBoxRect = GetRect(bounds, scaleAdjustment);

        //var rectArray = new RectInt32[] { SearchBoxRect };

        //var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(m_AppWindow.Id);
        //nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
    }
}