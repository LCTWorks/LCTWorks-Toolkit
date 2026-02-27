using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LCTWorks.WinUI.Controls;

[TemplateVisualState(Name = LoadingState, GroupName = CommonGroup)]
[TemplateVisualState(Name = LoadedState, GroupName = CommonGroup)]
[TemplateVisualState(Name = UnloadedState, GroupName = CommonGroup)]
[TemplateVisualState(Name = FailedState, GroupName = CommonGroup)]
[TemplatePart(Name = ImagePartName, Type = typeof(Image))]
[TemplatePart(Name = PlaceholderImagePartName, Type = typeof(Image))]
[TemplatePart(Name = RootGridPartName, Type = typeof(Grid))]
public partial class AdaptiveImage : Control
{
    private const string Base64Prefix = "base64,";
    private const string Base64Scheme = "data";
    private const string CommonGroup = "CommonStates";
    private const string FailedState = "Failed";
    private const string ImagePartName = "Image";
    private const string LoadedState = "Loaded";
    private const string LoadingState = "Loading";
    private const string PlaceholderImagePartName = "PlaceholderImage";
    private const string RootGridPartName = "RootGrid";
    private const string UnloadedState = "Unloaded";
    private static readonly Duration DefaultAnimationDuration = new(TimeSpan.FromMilliseconds(300));
    private Image? _image;
    private bool _isInVisualTree;

    #region Dependency Properties

    public static readonly DependencyProperty AnimationDurationProperty =
        DependencyProperty.Register(
            nameof(AnimationDuration),
            typeof(Duration),
            typeof(AdaptiveImage),
            new PropertyMetadata(DefaultAnimationDuration));

    public static readonly DependencyProperty NineGridProperty =
        DependencyProperty.Register(
            nameof(NineGrid),
            typeof(Thickness),
            typeof(AdaptiveImage),
            new PropertyMetadata(default(Thickness)));

    public static readonly DependencyProperty PlaceholderSourceProperty =
        DependencyProperty.Register(
            nameof(PlaceholderSource),
            typeof(ImageSource),
            typeof(AdaptiveImage),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PlaceholderStretchProperty =
        DependencyProperty.Register(
            nameof(PlaceholderStretch),
            typeof(Stretch),
            typeof(AdaptiveImage),
            new PropertyMetadata(Stretch.Uniform));

    public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
            nameof(Source),
            typeof(object),
            typeof(AdaptiveImage),
            new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(
            nameof(Stretch),
            typeof(Stretch),
            typeof(AdaptiveImage),
            new PropertyMetadata(Stretch.Uniform));

    /// <summary>
    /// Gets or sets the duration of the crossfade animation.
    /// </summary>
    public Duration AnimationDuration
    {
        get => (Duration)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    /// <summary>
    /// Gets or sets the nine-grid region for the main image.
    /// </summary>
    public Thickness NineGrid
    {
        get => (Thickness)GetValue(NineGridProperty);
        set => SetValue(NineGridProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder image shown while loading or on failure.
    /// </summary>
    public ImageSource? PlaceholderSource
    {
        get => (ImageSource?)GetValue(PlaceholderSourceProperty);
        set => SetValue(PlaceholderSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the stretch mode for the placeholder image.
    /// </summary>
    public Stretch PlaceholderStretch
    {
        get => (Stretch)GetValue(PlaceholderStretchProperty);
        set => SetValue(PlaceholderStretchProperty, value);
    }

    /// <summary>
    /// Gets or sets the image source to display.
    /// </summary>
    public object? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the stretch mode for the main image.
    /// </summary>
    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    #endregion Dependency Properties

    private CancellationTokenSource? _tokenSource;

    public AdaptiveImage()
    {
        DefaultStyleKey = typeof(AdaptiveImage);
    }

    public event EventHandler? ImageLoaded;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _image = GetTemplateChild(ImagePartName) as Image;
        _isInVisualTree = true;

        LoadSourceAsync(Source);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveImage control)
        {
            control.SourceChanged(e.NewValue);
        }
    }

    private void ApplyAnimationDuration()
    {
        var grid = GetTemplateChild(RootGridPartName) as Grid;
        if (grid == null)
        {
            return;
        }
        var groups = VisualStateManager.GetVisualStateGroups(grid);
        if (groups == null)
        {
            return;
        }

        foreach (var group in groups)
        {
            foreach (var state in group.States)
            {
                if (state.Name == LoadedState && state.Storyboard is not null)
                {
                    foreach (var child in state.Storyboard.Children)
                    {
                        child.Duration = AnimationDuration;
                    }

                    return;
                }
            }
        }
    }

    private void ClearImageSource()
    {
        TryRemoveImageHandlers();
        _image?.Source = null;
        GoToState(UnloadedState);
    }

    private void GoToState(string stateName)
    {
        if (stateName == LoadedState)
        {
            ApplyAnimationDuration();
        }

        VisualStateManager.GoToState(this, stateName, true);
    }

    private async void LoadSourceAsync(object? source)
    {
        if (_image is null || !_isInVisualTree)
        {
            return;
        }

        _tokenSource?.Cancel();
        _tokenSource = new CancellationTokenSource();

        ClearImageSource();
        if (source is null)
        {
            GoToState(UnloadedState);
            return;
        }

        if (source is ImageSource imageSource)
        {
            SetImageSource(imageSource);
            return;
        }
        try
        {
            await LoadSourceFromUnknownSourceAsync(source, _tokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            VisualStateManager.GoToState(this, FailedState, true);
        }
    }

    private async Task LoadSourceFromUnknownSourceAsync(object? source, CancellationToken token)
    {
        Uri? uri = source as Uri;
        if (uri == null)
        {
            var url = source as string ?? source?.ToString();
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
            {
                GoToState(FailedState);
                return;
            }
        }
        else
        {
            if (uri.Scheme != "http" && uri.Scheme != "https" && !uri.IsAbsoluteUri)
            {
                uri = new Uri("ms-appx:///" + uri.OriginalString.TrimStart('/'));
            }
        }
        BitmapImage? bitmapImage = null;
        if (uri != null)
        {
            if (string.Equals(uri.Scheme, Base64Scheme, StringComparison.OrdinalIgnoreCase))
            {
                var src = uri.OriginalString;

                var index = src.IndexOf(Base64Prefix);
                if (index >= 0)
                {
                    var bytes = Convert.FromBase64String(src[(index + Base64Prefix.Length)..]);
                    bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(new MemoryStream(bytes).AsRandomAccessStream());
                }
            }
            else
            {
                bitmapImage = new BitmapImage(uri)
                {
                    CreateOptions = BitmapCreateOptions.IgnoreImageCache
                };
            }
        }
        if (bitmapImage != null && !token.IsCancellationRequested)
        {
            SetImageSource(bitmapImage);
        }
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        TryRemoveImageHandlers();
        GoToState(FailedState);
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        TryRemoveImageHandlers();
        GoToState(LoadedState);
    }

    private void SetImageSource(ImageSource? source)
    {
        _image?.Source = source;
        if (source == null)
        {
            ClearImageSource();
        }
        if (source is BitmapImage bitmapImage)
        {
            bitmapImage.ImageOpened += OnImageOpened;
            bitmapImage.ImageFailed += OnImageFailed;
            //TODO: Loading
        }
        if (source is BitmapSource bitmapSource &&
                 bitmapSource.PixelHeight > 0 &&
                 bitmapSource.PixelWidth > 0)
        {
            GoToState(LoadedState);
        }
    }

    private void SourceChanged(object newSource)
    {
        if (!_isInVisualTree || _image is null)
        {
            return;
        }

        if (newSource is null)
        {
            ClearImageSource();
            return;
        }

        GoToState(LoadingState);
        LoadSourceAsync(newSource);
    }

    private void TryRemoveImageHandlers()
    {
        if (_image?.Source is BitmapImage bitmapImage)
        {
            bitmapImage.ImageOpened -= OnImageOpened;
            bitmapImage.ImageFailed -= OnImageFailed;
        }
    }
}