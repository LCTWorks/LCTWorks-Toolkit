using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace LCTWorks.WinUI.Controls.ContentDialogs;

public sealed partial class FeedbackContentDialog : ContentDialog
{
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(FeedbackContentDialog),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LinkItemsProperty =
            DependencyProperty.Register(nameof(LinkItems), typeof(ObservableCollection<FeedbackLinkItem>), typeof(FeedbackContentDialog),
            new PropertyMetadata(new ObservableCollection<FeedbackLinkItem>()));

    public FeedbackContentDialog()
    {
        InitializeComponent();
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public ObservableCollection<FeedbackLinkItem> LinkItems
    {
        get => (ObservableCollection<FeedbackLinkItem>)GetValue(LinkItemsProperty);
        set => SetValue(LinkItemsProperty, value);
    }
}