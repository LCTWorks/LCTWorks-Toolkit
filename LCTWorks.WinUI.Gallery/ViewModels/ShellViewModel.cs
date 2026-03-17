using CommunityToolkit.Mvvm.ComponentModel;
using LCTWorks.Workshop.Models;
using LCTWorks.Workshop.Services;
using System.Collections.ObjectModel;

namespace LCTWorks.Workshop.ViewModels;

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