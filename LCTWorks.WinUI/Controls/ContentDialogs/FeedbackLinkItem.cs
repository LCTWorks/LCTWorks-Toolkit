using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace LCTWorks.WinUI.Controls.ContentDialogs;

public partial class FeedbackLinkItem(string title, string description, string iconUri) : ObservableObject
{
    public ICommand? Command { get; set; }

    public object? CommandParameter { get; set; }

    public string Description { get; } = description;

    public string IconUri { get; } = iconUri;

    public string Title { get; } = title;
}