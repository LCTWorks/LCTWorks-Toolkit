using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace LCTWorks.WinUI.Controls;

[TemplateVisualState(Name = LoadingState, GroupName = CommonGroup)]
[TemplateVisualState(Name = LoadedState, GroupName = CommonGroup)]
[TemplateVisualState(Name = UnloadedState, GroupName = CommonGroup)]
[TemplateVisualState(Name = FailedState, GroupName = CommonGroup)]
[TemplatePart(Name = ImagePartName, Type = typeof(Image))]
[TemplatePart(Name = PlaceholderImagePartName, Type = typeof(Image))]
public partial class SmoothImage : Control
{
    private const string CommonGroup = "CommonStates";
    private const string FailedState = "Failed";
    private const string ImagePartName = "Image";
    private const string LoadedState = "Loaded";
    private const string LoadingState = "Loading";
    private const string PlaceholderImagePartName = "PlaceholderImage";
    private const string UnloadedState = "Unloaded";
    private Image? _image;
    private bool _isInVisualTree;

    public SmoothImage()
    {
        DefaultStyleKey = typeof(SmoothImage);
    }

    #region Dependency Properties

    public static readonly DependencyProperty AnimationDurationProperty =
        DependencyProperty.Register(
            nameof(AnimationDuration),
            typeof(TimeSpan),
            typeof(SmoothImage),
            new PropertyMetadata(TimeSpan.FromMilliseconds(300)));

    public static readonly DependencyProperty NineGridProperty =
        DependencyProperty.Register(
            nameof(NineGrid),
            typeof(Thickness),
            typeof(SmoothImage),
            new PropertyMetadata(default(Thickness)));

    public static readonly DependencyProperty PlaceholderSourceProperty =
        DependencyProperty.Register(
            nameof(PlaceholderSource),
            typeof(ImageSource),
            typeof(SmoothImage),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PlaceholderStretchProperty =
        DependencyProperty.Register(
            nameof(PlaceholderStretch),
            typeof(Stretch),
            typeof(SmoothImage),
            new PropertyMetadata(Stretch.Uniform));

    public static readonly DependencyProperty SourceProperty =
                        DependencyProperty.Register(
            nameof(Source),
            typeof(ImageSource),
            typeof(SmoothImage),
            new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(
            nameof(Stretch),
            typeof(Stretch),
            typeof(SmoothImage),
            new PropertyMetadata(Stretch.Uniform));

    /// <summary>
    /// Gets or sets the duration of the crossfade animation.
    /// </summary>
    public TimeSpan AnimationDuration
    {
        get => (TimeSpan)GetValue(AnimationDurationProperty);
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
    public ImageSource? Source
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

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Detach previous event handlers
        RemoveImageHandlers();

        _image = GetTemplateChild(ImagePartName) as Image;
        _isInVisualTree = true;

        SetImageSource(_image, Source);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SmoothImage control)
        {
            control.OnSourceChanged(e.NewValue as ImageSource);
        }
    }

    private void GoToState(string stateName)
    {
        VisualStateManager.GoToState(this, stateName, true);
    }

    private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        RemoveImageHandlers();
        GoToState(FailedState);
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        RemoveImageHandlers();
        GoToState(nameof(LoadedState));
    }

    private void OnSourceChanged(ImageSource? newSource)
    {
        if (!_isInVisualTree || _image is null)
        {
            return;
        }

        if (newSource is null)
        {
            RemoveImageHandlers();
            _image.Source = null;
            GoToState(nameof(Unloaded));
            return;
        }

        GoToState(nameof(LoadingState));
        SetImageSource(_image, newSource);
    }

    private void RemoveImageHandlers()
    {
        if (_image?.Source is BitmapImage bitmapImage)
        {
            bitmapImage.ImageOpened -= OnImageOpened;
            bitmapImage.ImageFailed -= OnImageFailed;
        }
    }

    private void SetImageSource(Image? image, ImageSource? source)
    {
        if (image is null)
        {
            return;
        }

        RemoveImageHandlers();

        if (source is null)
        {
            image.Source = null;
            GoToState(nameof(Unloaded));
            return;
        }

        // For BitmapImage sources, listen for load completion/failure
        if (source is BitmapImage bitmapImage)
        {
            bitmapImage.ImageOpened += OnImageOpened;
            bitmapImage.ImageFailed += OnImageFailed;
        }

        image.Source = source;

        // Non-BitmapImage sources (e.g., WriteableBitmap) are already loaded
        if (source is not BitmapImage)
        {
            GoToState(nameof(Loaded));
        }
    }
}