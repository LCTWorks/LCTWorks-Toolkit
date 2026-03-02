using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.Storage;

namespace LCTWorks.Workshop.Items;

public sealed partial class AdaptiveImagePage : ObservablePage
{
    private const string S1AppImageUri = "ms-appx:///Assets/Images/Sample800-1.jpg";
    private const string S1WebImageUri = "https://picsum.photos/800";

    public AdaptiveImagePage()
    {
        InitializeComponent();
        ShowPlaceholder = true;
        S1ImageSource = S1AppImageUri;
    }

    public object? S1ImageSource
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
            {
                LoadS1ImageUri(value);
            }
        }
    }

    public string? S1ImageSourceString
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public bool ShowPlaceholder
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
            {
                S1Img.PlaceholderSource = value ? new BitmapImage(new System.Uri("ms-appx:///Assets/Icons/AdaptiveImage.svg")) : null;
            }
        }
    }

    private void LoadS1ImageUri(object? source)
    {
        S1Img.Source = source;
        if (source is StorageFile file)
        {
            S1ImageSourceString = file.Path ?? file.FolderRelativeId;
            //Show path here.
        }
        else if (source is Uri uri)
        {
            S1ImageSourceString = uri.ToString();
        }
        else if (source is string str)
        {
            S1ImageSourceString = str;
        }
        else
        {
            S1ImageSourceString = null;
        }
    }

    private async void S1BrowseTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var result = await PickerHelper.OpenSingleFileAsync([".jpg", ".jpeg", ".png", ".bmp", ".gif"]);
        if (result != null)
        {
            LoadS1ImageUri(result);
        }
    }

    private void S1ImgImageFailed(object sender, System.EventArgs e) => S1NotificationRun.Text = "Loading failed.";

    private void S1ImgImageLoaded(object sender, System.EventArgs e) => S1NotificationRun.Text = "Loaded.";

    private void S1ImgImageLoading(object sender, System.EventArgs e) => S1NotificationRun.Text = "Loading...";

    private void S1LoadAppButtonTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        S1ImageSource = null;
        S1ImageSource = S1AppImageUri;
    }

    private void S1LoadWebButtonTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        S1ImageSource = null;
        S1ImageSource = S1WebImageUri;
    }
}