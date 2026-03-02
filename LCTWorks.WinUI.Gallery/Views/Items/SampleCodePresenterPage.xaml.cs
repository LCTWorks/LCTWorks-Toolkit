using CommunityToolkit.Mvvm.ComponentModel;
using LCTWorks.WinUI.Controls;
using LCTWorks.Workshop.ViewModels.Items;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class SampleCodePresenterPage : ObservablePage
{
    public SampleCodePresenterPage()
    {
        ViewModel = App.GetService<SampleCodePresenterViewModel>();
        InitializeComponent();
        MarkdownTabHeader = "Documentation";
        CodeExpanderHeader = "Code snippet";
    }

    public string CodeExpanderHeader
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public bool IsLoading
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public string MarkdownTabHeader
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public SampleCodePresenterViewModel? ViewModel { get; }
}