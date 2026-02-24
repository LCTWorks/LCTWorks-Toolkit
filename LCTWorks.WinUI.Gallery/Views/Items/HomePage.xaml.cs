using LCTWorks.WinUI.Gallery.ViewModels.Items;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Gallery.Views.Items;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
    }

    public HomeViewModel? ViewModel { get; private set; }
}