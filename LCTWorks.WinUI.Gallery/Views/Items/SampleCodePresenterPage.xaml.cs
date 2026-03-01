using LCTWorks.Workshop.ViewModels.Items;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class SampleCodePresenterPage : Page
{
    public SampleCodePresenterPage()
    {
        ViewModel = App.GetService<SampleCodePresenterViewModel>();
        InitializeComponent();
    }

    public SampleCodePresenterViewModel? ViewModel { get; }
}