using System.Linq;
using LCTWorks.WinUI.Extensions;
using LCTWorks.Workshop.ViewModels;
using LCTWorks.WinUI.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using LCTWorks.WinUI.Helpers;

namespace LCTWorks.Workshop.Views;

public sealed partial class ShellPage : Page
{
    private readonly FrameNavigationService _navigationService;

    public ShellPage(
        ShellViewModel viewModel,
        FrameNavigationService navigationService)
    {
        ViewModel = viewModel;
        _navigationService = navigationService;
        InitializeComponent();

        navigationService.Frame = NavigationFrame;
        NavigationViewHelper.Configure(navigationService, NavigationViewControl, typeof(SettingsViewModel).FullName!);
        TitleBarHelper.Extend(AppTitleBar, NavigationViewControl, navigationService);
    }

    public string AppDisplayName => "AppDisplayName".GetTextLocalized();

    public ShellViewModel ViewModel { get; private set; }

    private void NavigationViewControl_Loaded(object sender, RoutedEventArgs __)
    {
        var settings = ((NavigationView)sender).SettingsItem as NavigationViewItem;
        settings?.FontFamily = new FontFamily("ms-appx:///Assets/Fonts/Poppins-Regular.ttf#Poppins");

        NavigateToFirstItem();
    }

    /// <summary>
    /// Opens the first navigable item (Home) on startup. The navigation key is passed as the parameter,
    /// so <see cref="NavigationViewHelper"/> selects the matching menu item (see DocItem.IsMatch).
    /// </summary>
    private void NavigateToFirstItem()
    {
        if (NavigationFrame.Content != null)
        {
            return;
        }
        var first = ViewModel.ItemsSource.FirstOrDefault(item => !item.IsHeader);
        if (first != null)
        {
            _navigationService.NavigateTo(first.NavigationKey, first.NavigationKey, clearNavigation: true);
        }
    }
}
