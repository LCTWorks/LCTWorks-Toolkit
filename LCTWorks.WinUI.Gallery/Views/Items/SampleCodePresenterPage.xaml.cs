using CommunityToolkit.Mvvm.ComponentModel;
using LCTWorks.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class SampleCodePresenterPage : ObservablePage
{
    public SampleCodePresenterPage()
    {
        InitializeComponent();
        MarkdownTabHeader = "Documentation";
        CodeExpanderHeader = "Code snippet";
    }

    public string CodeExpanderHeader
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsLoading
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string MarkdownTabHeader
    {
        get;
        set => SetProperty(ref field, value);
    }
}