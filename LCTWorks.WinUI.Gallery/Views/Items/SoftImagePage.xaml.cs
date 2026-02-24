using LCTWorks.WinUI.Gallery.ViewModels.Items;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Gallery.Views.Items;

public sealed partial class SoftImagePage : Page
{
    public SoftImagePage()
    {
        ViewModel = App.GetService<SoftImageViewModel>();
        InitializeComponent();
    }

    public SoftImageViewModel? ViewModel { get; private set; }
}