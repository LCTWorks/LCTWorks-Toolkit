using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using System;
using System.Windows.Input;

namespace LCTWorks.WinUI.Controls.ContentDialogs;

public partial class ItemsContentDialogItem : ObservableObject
{
    private string? _iconUri;
    private bool _showOpenExternal = true;

    public ItemsContentDialogItem(string title, string description, string? iconUri = null)
    {
        Description = description;
        Title = title;
        IconUri = iconUri;
    }

    [ObservableProperty]
    public partial ICommand? Command { get; set; }

    [ObservableProperty]
    public partial object? CommandParameter { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    /// <summary>
    /// Supports both Fluent icon glyphs and image URIs.
    /// </summary>
    public string? IconUri
    {
        get => _iconUri;
        set
        {
            if (SetProperty(ref _iconUri, value))
            {
                if (string.IsNullOrWhiteSpace(_iconUri))
                {
                    return;
                }
                bool isImage;
                try
                {
                    var uri = new Uri(_iconUri, UriKind.RelativeOrAbsolute);
                    isImage = uri.Scheme == "ms-appx" || uri.Scheme == "https" || uri.Scheme == "http";
                }
                catch
                {
                    isImage = false;
                }
                IsImage = isImage;
                OnPropertyChanged(nameof(IsGlyph));
            }
        }
    }

    public bool ShowOpenExternalIcon
    {
        get => _showOpenExternal;
        set => SetProperty(ref _showOpenExternal, value);
    }

    [ObservableProperty]
    public partial string Title { get; set; }

    internal bool IsGlyph => !IsImage;

    [ObservableProperty]
    internal partial bool IsImage { get; private set; }
}