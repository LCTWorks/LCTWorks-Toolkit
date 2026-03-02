using LCTWorks.Workshop.ViewModels.Items;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.Workshop.Items;

public sealed partial class AdaptiveImagePage : Page
{
    public AdaptiveImagePage()
    {
        ViewModel = App.GetService<AdaptiveImageViewModel>();
        InitializeComponent();
    }

    public AdaptiveImageViewModel? ViewModel { get; private set; }
}