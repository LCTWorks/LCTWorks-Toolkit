using LCTWorks.Workshop.ViewModels.Items;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        ViewModel = App.GetService<HomeViewModel>();
        InitializeComponent();
    }

    public HomeViewModel? ViewModel { get; private set; }
}