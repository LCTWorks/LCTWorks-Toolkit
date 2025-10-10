using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace LCTWorks.WinUI.Controls.ContentDialogs;

public partial class FeedbackLinkItem(string title, string description, string iconUri, string url) : ObservableObject
{
    public string Description { get; } = description;

    public string IconUri { get; } = iconUri;

    public string Title { get; } = title;

    public string Url { get; } = url;

    [RelayCommand]
    private void OpenLink()
    {
        try
        {
            var uri = new Uri(Url);
            _ = Windows.System.Launcher.LaunchUriAsync(uri);
        }
        catch
        {
        }
    }
}