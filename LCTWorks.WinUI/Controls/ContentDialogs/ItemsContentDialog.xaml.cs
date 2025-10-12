using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace LCTWorks.WinUI.Controls.ContentDialogs;

public sealed partial class ItemsContentDialog : ContentDialog
{
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(ItemsContentDialog),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HideOnCommandExecutedProperty =
        DependencyProperty.Register(nameof(HideOnCommandExecuted), typeof(bool), typeof(ItemsContentDialog),
            new PropertyMetadata(true));

    public static readonly DependencyProperty LinkItemsProperty =
                DependencyProperty.Register(nameof(LinkItems), typeof(ObservableCollection<ItemsContentDialogItem>), typeof(ItemsContentDialog),
            new PropertyMetadata(new ObservableCollection<ItemsContentDialogItem>()));

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

    public ObservableCollection<ItemsContentDialogItem> LinkItems
    {
        get => (ObservableCollection<ItemsContentDialogItem>)GetValue(LinkItemsProperty);
        set => SetValue(LinkItemsProperty, value);
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