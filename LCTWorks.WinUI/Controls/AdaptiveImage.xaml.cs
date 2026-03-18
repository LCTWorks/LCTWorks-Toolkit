using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;

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
    private static readonly Color DefaultDarkFillColor = Color.FromArgb(255, 255, 255, 255);
    private static readonly Color DefaultLightFillColor = Color.FromArgb(255, 0, 0, 0);
    private Image? _image;
    private bool _isInVisualTree;
    private string? _svgContent;

    #region Dependency Properties

    public static readonly DependencyProperty AnimationDurationProperty =
        DependencyProperty.Register(
            nameof(AnimationDuration),
            typeof(Duration),
            typeof(AdaptiveImage),
            new PropertyMetadata(DefaultAnimationDuration));

    public static readonly DependencyProperty DarkThemeSVGFillColorProperty =
        DependencyProperty.Register(
            nameof(DarkThemeSVGFillColor),
            typeof(Color),
            typeof(AdaptiveImage),
            new PropertyMetadata(DefaultDarkFillColor, OnSvgFillColorChanged));

    public static readonly DependencyProperty EnableSvgColorOverrideProperty =
        DependencyProperty.Register(
            nameof(EnableSvgColorOverride),
            typeof(bool),
            typeof(AdaptiveImage),
            new PropertyMetadata(true, OnSvgColorOverrideChanged));

    public static readonly DependencyProperty LightThemeSVGFillColorProperty =
        DependencyProperty.Register(
            nameof(LightThemeSVGFillColor),
            typeof(Color),
            typeof(AdaptiveImage),
            new PropertyMetadata(DefaultLightFillColor, OnSvgFillColorChanged));

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

    public Color DarkThemeSVGFillColor
    {
        get => (Color)GetValue(DarkThemeSVGFillColorProperty);
        set => SetValue(DarkThemeSVGFillColorProperty, value);
    }

    public bool EnableSvgColorOverride
    {
        get => (bool)GetValue(EnableSvgColorOverrideProperty);
        set => SetValue(EnableSvgColorOverrideProperty, value);
    }

    public Color LightThemeSVGFillColor
    {
        get => (Color)GetValue(LightThemeSVGFillColorProperty);
        set => SetValue(LightThemeSVGFillColorProperty, value);
    }

    public Thickness NineGrid
    {
        get => (Thickness)GetValue(NineGridProperty);
        set => SetValue(NineGridProperty, value);
    }

    public ImageSource? PlaceholderSource
    {
        get => (ImageSource?)GetValue(PlaceholderSourceProperty);
        set => SetValue(PlaceholderSourceProperty, value);
    }

    public Stretch PlaceholderStretch
    {
        get => (Stretch)GetValue(PlaceholderStretchProperty);
        set => SetValue(PlaceholderStretchProperty, value);
    }

    public object? Source
    {
        get => (object?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

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

    public event EventHandler? ImageFailed;

    public event EventHandler? ImageLoaded;

    public event EventHandler? ImageLoading;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        ActualThemeChanged -= OnActualThemeChanged;
        _image = GetTemplateChild(ImagePartName) as Image;
        _isInVisualTree = true;
        ActualThemeChanged += OnActualThemeChanged;

        LoadSourceAsync(Source);
    }

    private static bool IsSvgContent(string content)
    {
        var trimmed = content.TrimStart();
        return trimmed.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
               (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) &&
                trimmed.Contains("<svg", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSvgUri(Uri uri)
    {
        return uri.LocalPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveImage control)
        {
            control.SourceChanged(e.NewValue);
        }
    }

    private static void OnSvgColorOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveImage control && control._svgContent != null)
        {
            control.ReapplySvgSource();
        }
    }

    private static void OnSvgFillColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveImage control && control._svgContent != null && control.EnableSvgColorOverride)
        {
            control.ReapplySvgSource();
        }
    }

    private static string ReplaceSvgPathFills(string svgContent, Color color)
    {
        var colorHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        var doc = XDocument.Parse(svgContent);

        foreach (var element in doc.Descendants())
        {
            if (string.Equals(element.Name.LocalName, "path", StringComparison.OrdinalIgnoreCase))
            {
                var fillAttr = element.Attributes()
                    .FirstOrDefault(a => string.Equals(a.Name.LocalName, "fill", StringComparison.OrdinalIgnoreCase));

                if (fillAttr != null)
                {
                    fillAttr.Value = colorHex;
                }
                else
                {
                    element.SetAttributeValue("fill", colorHex);
                }
            }
        }

        return doc.ToString(SaveOptions.DisableFormatting);
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

    private async Task ApplySvgSourceAsync(string svgContent, CancellationToken token)
    {
        if (_image is null)
        {
            return;
        }

        ImageLoading?.Invoke(this, EventArgs.Empty);
        GoToState(LoadingState);

        var processedSvg = EnableSvgColorOverride
            ? ReplaceSvgPathFills(svgContent, GetCurrentThemeFillColor())
            : svgContent;

        try
        {
            var svgSource = new SvgImageSource();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(processedSvg));
            var result = await svgSource.SetSourceAsync(stream.AsRandomAccessStream());

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (result == SvgImageSourceLoadStatus.Success)
            {
                _image.Source = svgSource;
                GoToLoadedState();
            }
            else
            {
                GoToFailedState();
            }
        }
        catch
        {
            if (!token.IsCancellationRequested)
            {
                GoToFailedState();
            }
        }
    }

    private void ClearImageSource()
    {
        TryRemoveImageHandlers();
        _image?.Source = null;
        _svgContent = null;
        GoToUnloadedState();
    }

    private Color GetCurrentThemeFillColor()
    {
        return ActualTheme == ElementTheme.Dark ? DarkThemeSVGFillColor : LightThemeSVGFillColor;
    }

    private void GoToFailedState()
    {
        GoToState(FailedState);
        ImageFailed?.Invoke(this, EventArgs.Empty);
    }

    private void GoToLoadedState()
    {
        GoToState(LoadedState);
        ImageLoaded?.Invoke(this, EventArgs.Empty);
    }

    private void GoToState(string stateName)
    {
        if (stateName == LoadedState)
        {
            ApplyAnimationDuration();
        }

        VisualStateManager.GoToState(this, stateName, true);
    }

    private void GoToUnloadedState()
    {
        GoToState(UnloadedState);
        ImageLoaded?.Invoke(this, EventArgs.Empty);
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
            GoToUnloadedState();
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
            GoToFailedState();
        }
    }

    private async Task LoadSourceFromUnknownSourceAsync(object? source, CancellationToken token)
    {
        if (source is string svgString && IsSvgContent(svgString))
        {
            _svgContent = svgString;
            await ApplySvgSourceAsync(svgString, token);
            return;
        }

        Uri? uri = source as Uri;
        if (uri == null)
        {
            var url = source as string ?? source?.ToString();
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
            {
                GoToFailedState();
                return;
            }
        }

        if (uri != null && !uri.IsAbsoluteUri)
        {
            uri = new Uri("ms-appx:///" + uri.OriginalString.TrimStart('/'));
        }
        else if (uri != null && uri.IsAbsoluteUri &&
                 uri.Scheme != "http" && uri.Scheme != "https" &&
                 uri.Scheme != "ms-appx" && uri.Scheme != "ms-appdata" &&
                 uri.Scheme != "file" && uri.Scheme != Base64Scheme)
        {
            uri = new Uri("ms-appx:///" + uri.OriginalString.TrimStart('/'));
        }

        if (uri != null && uri.IsAbsoluteUri && IsSvgUri(uri))
        {
            await LoadSvgFromUriAsync(uri, token);
            return;
        }

        if (source is StorageFile svgFile &&
            string.Equals(svgFile.FileType, ".svg", StringComparison.OrdinalIgnoreCase))
        {
            await LoadSvgFromFileAsync(svgFile, token);
            return;
        }

        BitmapImage? bitmapImage = null;
        if (uri != null && uri.IsAbsoluteUri)
        {
            if (string.Equals(uri.Scheme, Base64Scheme, StringComparison.OrdinalIgnoreCase))
            {
                var src = uri.OriginalString;

                var index = src.IndexOf(Base64Prefix);
                if (index >= 0)
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(src[(index + Base64Prefix.Length)..]);
                        bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(new MemoryStream(bytes).AsRandomAccessStream());
                    }
                    catch
                    {
                        GoToFailedState();
                        return;
                    }
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
        if (bitmapImage == null)
        {
            IRandomAccessStream? stream = null;
            if (source is IRandomAccessStream s)
            {
                stream = s;
            }
            else if (source is StorageFile file)
            {
                stream = await file.OpenReadAsync();
            }
            if (stream != null)
            {
                try
                {
                    bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);
                }
                catch (Exception)
                {
                    GoToFailedState();
                    return;
                }
            }
        }
        if (bitmapImage != null && !token.IsCancellationRequested)
        {
            SetImageSource(bitmapImage);
        }
    }

    private async Task LoadSvgFromFileAsync(StorageFile file, CancellationToken token)
    {
        try
        {
            var content = await FileIO.ReadTextAsync(file);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _svgContent = content;
            await ApplySvgSourceAsync(content, token);
        }
        catch
        {
            if (!token.IsCancellationRequested)
            {
                GoToFailedState();
            }
        }
    }

    private async Task LoadSvgFromUriAsync(Uri uri, CancellationToken token)
    {
        try
        {
            StorageFile file;
            if (string.Equals(uri.Scheme, "file", StringComparison.OrdinalIgnoreCase))
            {
                file = await StorageFile.GetFileFromPathAsync(uri.LocalPath);
            }
            else
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            }

            await LoadSvgFromFileAsync(file, token);
        }
        catch
        {
            if (!token.IsCancellationRequested)
            {
                GoToFailedState();
            }
        }
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (_svgContent != null && EnableSvgColorOverride && _isInVisualTree)
        {
            ReapplySvgSource();
        }
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        TryRemoveImageHandlers();
        GoToFailedState();
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        TryRemoveImageHandlers();
        GoToLoadedState();
    }

    private void OnSvgImageFailed(SvgImageSource sender, SvgImageSourceFailedEventArgs args)
    {
        TryRemoveImageHandlers();
        GoToFailedState();
    }

    private void OnSvgImageOpened(SvgImageSource sender, SvgImageSourceOpenedEventArgs args)
    {
        TryRemoveImageHandlers();
        GoToLoadedState();
    }

    private void ReapplySvgSource()
    {
        if (_svgContent is null || _image is null)
        {
            return;
        }

        _tokenSource?.Cancel();
        _tokenSource = new CancellationTokenSource();
        _ = ApplySvgSourceAsync(_svgContent, _tokenSource.Token);
    }

    private void SetImageSource(ImageSource? source)
    {
        ImageLoading?.Invoke(this, EventArgs.Empty);
        GoToState(LoadingState);

        _image?.Source = source;
        if (source == null)
        {
            ClearImageSource();
        }
        if (source is BitmapImage bitmapImage)
        {
            bitmapImage.ImageOpened += OnImageOpened;
            bitmapImage.ImageFailed += OnImageFailed;
        }
        if (source is SvgImageSource svgImage)
        {
            svgImage.Opened += OnSvgImageOpened;
            svgImage.OpenFailed += OnSvgImageFailed;
        }
        if (source is BitmapSource bitmapSource &&
                 bitmapSource.PixelHeight > 0 &&
                 bitmapSource.PixelWidth > 0)
        {
            GoToLoadedState();
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

        LoadSourceAsync(newSource);
    }

    private void TryRemoveImageHandlers()
    {
        if (_image?.Source is BitmapImage bitmapImage)
        {
            bitmapImage.ImageOpened -= OnImageOpened;
            bitmapImage.ImageFailed -= OnImageFailed;
        }
        if (_image?.Source is SvgImageSource svgImage)
        {
            svgImage.Opened -= OnSvgImageOpened;
            svgImage.OpenFailed -= OnSvgImageFailed;
        }
    }
}