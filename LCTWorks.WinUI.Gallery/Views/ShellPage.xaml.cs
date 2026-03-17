using LCTWorks.WinUI.Experimental.Helpers;
using LCTWorks.WinUI.Extensions;
using LCTWorks.Workshop.ViewModels;
using LCTWorks.WinUI.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LCTWorks.Workshop.Views;

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
        TitleBarHelperExperimental.Extend(AppTitleBar, NavigationViewControl, navigationService);
    }

    public string AppDisplayName => "AppDisplayName".GetTextLocalized();

    public ShellViewModel ViewModel { get; private set; }

    private void NavigationViewControl_Loaded(object sender, RoutedEventArgs __)
    {
        var settings = ((NavigationView)sender).SettingsItem as NavigationViewItem;
        settings?.FontFamily = new FontFamily("ms-appx:///Assets/Fonts/Poppins-Regular.ttf#Poppins");
    }
}