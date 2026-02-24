using CommunityToolkit.Mvvm.ComponentModel;
using LCTWorks.WinUI.Gallery.Models;
using LCTWorks.WinUI.Gallery.Services;
using System.Collections.ObjectModel;

namespace LCTWorks.WinUI.Gallery.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly DocsService _docsService;

    public ShellViewModel(DocsService service)
    {
        _docsService = service;
        ItemsSource = new ObservableCollection<DocItem>(_docsService.Items);
    }

    [ObservableProperty]
    public partial bool IsBackEnabled { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DocItem> ItemsSource
    {
        get; set;
    } = new ObservableCollection<DocItem>();

    [ObservableProperty]
    public partial object? Selected
    {
        get; set;
    }
}