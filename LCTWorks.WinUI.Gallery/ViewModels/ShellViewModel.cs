using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using LCTWorks.WinUI.Gallery.ViewModels.Controls;

namespace LCTWorks.WinUI.Gallery.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    public ShellViewModel()
    {
        ItemsSource = new AdvancedCollectionView();
        ItemsSource.Add(typeof(SoftImageViewModel).Name!);
    }

    [ObservableProperty]
    public partial bool IsBackEnabled { get; set; }

    [ObservableProperty]
    public partial AdvancedCollectionView? ItemsSource
    {
        get; set;
    }

    [ObservableProperty]
    public partial object? Selected
    {
        get; set;
    }
}