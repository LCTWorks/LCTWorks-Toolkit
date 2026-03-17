using LCTWorks.Workshop.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.Workshop.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }

    public SettingsViewModel? ViewModel { get; private set; }
}