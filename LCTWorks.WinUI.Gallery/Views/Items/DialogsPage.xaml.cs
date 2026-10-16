using CommunityToolkit.Mvvm.Input;
using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Controls.ContentDialogs;
using LCTWorks.WinUI.Dialogs;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Models;
using System.Collections.Generic;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class DialogsPage : ObservablePage
{
    private static readonly List<ItemsContentDialogItem> DialogItems =
    [
        new ItemsContentDialogItem
        {
            IconUri = "",
            IconType = ItemsContentDialogIconType.FluentIcon,
            Title = "Highlighted item",
            Description = "Lorem ipsum dolor sit amet.",
            Caption = "HIGHLIGHT",
            IsHighlighted = true,
            ShowOpenExternalIcon = true,
            Command = new RelayCommand(() => { }),
        },
        new ItemsContentDialogItem
        {
            IconUri = "bolt",
            IconType = ItemsContentDialogIconType.MaterialSymbol,
            Title = "Highlighted item",
            Description = "Lorem ipsum dolor sit amet.",
            Caption = "PRO",
            ShowOpenExternalIcon = true,
            Command = new RelayCommand(() => { }),
        },
        new ItemsContentDialogItem
        {
            IconUri = "",
            IconType = ItemsContentDialogIconType.FluentIcon,
            Title = "Interactive item",
            Description = "with a regular description",
            ShowOpenExternalIcon = true,
            Command = new RelayCommand(() => { }),
        },
    ];

    private readonly DialogService? _dialogService;

    public DialogsPage()
    {
        InitializeComponent();
        _dialogService = App.GetService<DialogService>();
    }

    public string Description { get; } = "Dialogs_Description".GetTextLocalized();

    public string Header { get; } = "Dialogs_Title".GetTextLocalized();

    private async void ShowItemsContentDialogAsync(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new ItemsContentDialog
        {
            Title = "Title",
            Description = "Description (optional)",
            PrimaryButtonText = "Primary button",
            Items = [with(DialogItems)],
        };
        _dialogService?.ShowDialogAsync(dialog);
    }
}