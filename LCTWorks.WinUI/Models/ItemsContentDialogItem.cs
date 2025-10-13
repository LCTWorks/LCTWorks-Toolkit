using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Input;

namespace LCTWorks.WinUI.Models;

public partial class ItemsContentDialogItem : ObservableObject
{
    private ItemsContentDialogIconType? _iconType;
    private string? _iconUri;
    private bool _showOpenExternal = true;

    [ObservableProperty]
    public partial ICommand? Command { get; set; }

    [ObservableProperty]
    public partial object? CommandParameter { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    public ItemsContentDialogIconType IconType
    {
        get => _iconType ?? CalculateIconType();
        set
        {
            if (SetProperty(ref _iconType, value))
            {
                OnPropertyChanged(nameof(IsImage));
                OnPropertyChanged(nameof(IsFluentIcon));
                OnPropertyChanged(nameof(IsMaterialSymbol));
                OnPropertyChanged(nameof(IsFontAwesomeBrand));
            }
        }
    }

    /// <summary>
    /// Supports both icon glyphs and image URIs.
    /// </summary>
    public string? IconUri
    {
        get => _iconUri;
        set => SetProperty(ref _iconUri, value);
    }

    public bool ShowOpenExternalIcon
    {
        get => _showOpenExternal;
        set => SetProperty(ref _showOpenExternal, value);
    }

    [ObservableProperty]
    public partial string Title { get; set; }

    internal bool IsFluentIcon => IconType == ItemsContentDialogIconType.FluentIcon;

    internal bool IsFontAwesomeBrand => IconType == ItemsContentDialogIconType.FontAwesomeBrand;

    internal bool IsImage => IconType == ItemsContentDialogIconType.Image;

    internal bool IsMaterialSymbol => IconType == ItemsContentDialogIconType.MaterialSymbol;

    private ItemsContentDialogIconType CalculateIconType()
    {
        if (string.IsNullOrWhiteSpace(_iconUri))
        {
            return ItemsContentDialogIconType.None;
        }
        try
        {
            var uri = new Uri(_iconUri, UriKind.RelativeOrAbsolute);
            if (uri.Scheme == "ms-appx" || uri.Scheme == "https" || uri.Scheme == "http")
            {
                return ItemsContentDialogIconType.Image;
            }
        }
        catch
        {
        }
        return ItemsContentDialogIconType.FluentIcon;
    }
}

public enum ItemsContentDialogIconType
{
    None,
    FluentIcon,
    MaterialSymbol,
    FontAwesomeBrand,
    Image,
}