using LCTWorks.WinUI.Models;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace LCTWorks.WinUI.Controls.ContentDialogs;

public sealed partial class ItemsContentDialog : ContentDialog
{
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(ItemsContentDialog),
            new PropertyMetadata(string.Empty, DescriptionPropertyChanged));

    public static readonly DependencyProperty HideOnCommandExecutedProperty =
        DependencyProperty.Register(nameof(HideOnCommandExecuted), typeof(bool), typeof(ItemsContentDialog),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ItemsProperty =
                DependencyProperty.Register(nameof(Items), typeof(ObservableCollection<ItemsContentDialogItem>), typeof(ItemsContentDialog),
            new PropertyMetadata(new ObservableCollection<ItemsContentDialogItem>()));

    internal static readonly DependencyProperty HasDescriptionProperty =
        DependencyProperty.Register(nameof(HasDescription), typeof(bool), typeof(ItemsContentDialog),
            new PropertyMetadata(false));

    public ItemsContentDialog()
    {
        InitializeComponent();
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public bool HideOnCommandExecuted
    {
        get => (bool)GetValue(HideOnCommandExecutedProperty);
        set => SetValue(HideOnCommandExecutedProperty, value);
    }

    public ObservableCollection<ItemsContentDialogItem> Items
    {
        get => (ObservableCollection<ItemsContentDialogItem>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    internal bool HasDescription
    {
        get => (bool)GetValue(HasDescriptionProperty);
        set => SetValue(HasDescriptionProperty, value);
    }

    private static void DescriptionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ItemsContentDialog dialog)
        {
            dialog.HasDescription = !string.IsNullOrWhiteSpace((string)e.NewValue);
        }
    }

    private void ItemCommandTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (HideOnCommandExecuted)
        {
            Hide();
        }
    }

    private void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button)
        {
            if (button.Command != null)
            {
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            }
        }
    }

    private void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }
}