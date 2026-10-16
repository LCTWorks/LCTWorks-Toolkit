using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using LCTWorks.Web;
using LCTWorks.Web.Tools;
using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Extensions;
using LCTWorks.Workshop.Models;
using LCTWorks.Workshop.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class WebToolsPage : ObservablePage
{
    private CancellationTokenSource? _cts;
    private bool _isReportOpen;
    private WebPreviewer? _previewer;
    private readonly WebView2PageFetcher _webViewFetcher;

    public WebToolsPage()
    {
        InitializeComponent();
        _webViewFetcher = new WebView2PageFetcher(FetcherWebView);
    }

    public bool BypassCache
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public PreviewCard? Card
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public ObservableCollection<PropertyRow> CrawlerRows { get; } = [];

    public string Description { get; } = "WebTools_Description".GetTextLocalized();

    public string? ErrorText
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string Header { get; } = "WebTools_Title".GetTextLocalized();

    public ObservableCollection<ImageItem> Images { get; } = [];

    public string ImagesHeader
    {
        get;
        private set => SetProperty(ref field, value);
    } = "Images";

    public string MetaHeader
    {
        get;
        private set => SetProperty(ref field, value);
    } = "Meta tags";

    public ObservableCollection<PropertyRow> MetaRows { get; } = [];

    public ObservableCollection<PropertyRow> PreviewRows { get; } = [];

    public bool RetryAsPreviewBot
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _previewer = null; // options changed, rebuild on next run
            }
        }
    } = true;

    /// <summary>
    /// Sample URLs shown as an icon grid. Pass an icon path (ms-appx:///Assets/Icons/Brands/...) as the third argument.
    /// </summary>
    public SampleLink[] Samples { get; } =
    [
        new("YouTube", "https://www.youtube.com/watch?v=dQw4w9WgXcQ", "ms-appx:///Assets/Icons/Brands/youtube.svg"),
        new("youtu.be", "https://youtu.be/jNQXAC9IVRw", "ms-appx:///Assets/Icons/Brands/youtube.svg"),
        new("Reddit", "https://www.reddit.com/r/dotnet/", "ms-appx:///Assets/Icons/Brands/reddit.svg"),
        new("Amazon", "https://www.amazon.com/dp/0735211299", "ms-appx:///Assets/Icons/Brands/amazon.svg"),
        new("X", "https://x.com/dotnet", "ms-appx:///Assets/Icons/Brands/twitter-x.svg"),
        new("Vimeo", "https://vimeo.com/76979871", "ms-appx:///Assets/Icons/Brands/vimeo.svg"),
        new("Stack Overflow", "https://stackoverflow.com/questions/11227809", "ms-appx:///Assets/Icons/Brands/stackoverflow.svg"),
        new("GitHub", "https://github.com/dotnet/runtime", "ms-appx:///Assets/Icons/Brands/github.svg"),
        new("MS Learn", "https://learn.microsoft.com/dotnet/", "ms-appx:///Assets/Icons/Brands/microsoft.svg"),
        new("BBC News", "https://www.bbc.com/news", "ms-appx:///Assets/Icons/Brands/bbc.svg"),
        new("Image link", "https://picsum.photos/800", "ms-appx:///Assets/Icons/Brands/image.svg"),
    ];

    /// <summary>
    /// Load blocked pages (e.g. Cloudflare "Just a moment...") in a hidden WebView2 and parse the rendered HTML.
    /// </summary>
    public bool UseWebViewFallback
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _previewer = null; // options changed, rebuild on next run
            }
        }
    } = true;

    public string? StatusText
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string? UriSourceString
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                StartCrawling();
            }
        }
    }

    public bool ValidateImages
    {
        get;
        set => SetProperty(ref field, value);
    }

    #region x:Bind helpers

    public static Visibility HasText(string? value)
        => string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;

    public static bool HasTextBool(string? value)
        => !string.IsNullOrWhiteSpace(value);

    public static Visibility IsNotNull(object? value)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public static bool IsNotNullBool(object? value)
            => value is not null;

    public static Visibility IsNull(object? value)
        => value is null ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility IsTrue(bool value)
        => value ? Visibility.Visible : Visibility.Collapsed;

    #endregion x:Bind helpers

    private static void AddRows<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        ObservableCollection<PropertyRow> target, T? source, bool includeEmpty = false)
    {
        if (source is null)
        {
            return;
        }
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }
            var value = FormatValue(property.GetValue(source));
            if (value != null || includeEmpty)
            {
                target.Add(new PropertyRow(property.Name, value ?? "—"));
            }
        }
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        string s => string.IsNullOrWhiteSpace(s) ? null : s,
        bool b => b ? "true" : "false",
        IEnumerable e => string.Join(", ", e.Cast<object?>().Select(x => x?.ToString())),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture),
    };

    private static string NormalizeUrl(string url)
        => url.Contains("://", StringComparison.Ordinal) ? url : "https://" + url.TrimStart('/');

    private string BuildReportText()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"URL: {UriSourceString}");
        if (!string.IsNullOrWhiteSpace(StatusText))
        {
            builder.AppendLine(StatusText);
        }

        void AppendSection(string title, ObservableCollection<PropertyRow> rows)
        {
            builder.AppendLine().AppendLine($"## {title}");
            foreach (var row in rows)
            {
                builder.AppendLine($"{row.Name}: {row.Value}");
            }
        }

        AppendSection("WebPreview", PreviewRows);
        AppendSection("WebCrawler", CrawlerRows);
        AppendSection("Meta tags", MetaRows);

        builder.AppendLine().AppendLine($"## Images ({Images.Count})");
        foreach (var image in Images)
        {
            builder.AppendLine($"{image.Src} ({image.SizeText})");
        }
        return builder.ToString();
    }

    private void ClearResults()
    {
        Card = null;
        ErrorText = null;
        StatusText = null;
        PreviewRows.Clear();
        CrawlerRows.Clear();
        MetaRows.Clear();
        Images.Clear();
        MetaHeader = "Meta tags";
        ImagesHeader = "Images";
    }

    /// <summary>
    /// Sets <see cref="UriSourceString"/>; re-runs the crawl when the URL didn't change (refresh).
    /// </summary>
    private void CommitUrl(string? url)
    {
        if (string.Equals(UriSourceString, url, StringComparison.Ordinal))
        {
            StartCrawling();
        }
        else
        {
            UriSourceString = url;
        }
    }

    private void FillCrawlerRows(WebCrawler crawler)
    {
        void Add(string name, object? value) => CrawlerRows.Add(new PropertyRow(name, FormatValue(value) ?? "—"));

        Add(nameof(WebCrawler.SourceUrl), crawler.SourceUrl);
        Add(nameof(WebCrawler.BaseUri), crawler.BaseUri);
        Add(nameof(WebCrawler.StatusCode), crawler.StatusCode is { } status ? $"{(int)status} {status}" : null);
        Add(nameof(WebCrawler.ContentType), crawler.ContentType);
        Add(nameof(WebCrawler.IsLoaded), crawler.IsLoaded);
        Add(nameof(WebCrawler.IsBlocked), crawler.IsBlocked);
        Add(nameof(WebCrawler.BlockReason), crawler.BlockReason);
        Add("GetTitle()", crawler.GetTitle());
        Add("GetDescription()", crawler.GetDescription());
        Add("GetSiteName()", crawler.GetSiteName());
        Add("GetPreviewImage()", crawler.GetPreviewImage());
        Add("GetIconUrl()", crawler.GetIconUrl());
        Add("GetOEmbedUrl()", crawler.GetOEmbedUrl());
    }

    private WebPreviewer GetPreviewer()
        => _previewer ??= new WebPreviewer(new WebPreviewerOptions
        {
            RetryAsPreviewBot = RetryAsPreviewBot,
            BlockedPageFetcher = UseWebViewFallback ? _webViewFetcher.FetchHtmlAsync : null,
        });

    private async void OpenFinalUrlClick(object sender, RoutedEventArgs e)
    {
        var url = Card?.LinkText;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            await Launcher.LaunchUriAsync(uri);
        }
    }

    private void PreviewButtonClick(object sender, RoutedEventArgs e)
        => CommitUrl(UrlTextBox.Text);

    private void PreviewImageFailed(object? sender, EventArgs e)
    {
        if (Card?.ImageUrl is { } url)
        {
            StatusText = $"{StatusText}  ·  Thumbnail failed to load: {url}";
        }
    }

    private void ReportDialogCopyClick(Microsoft.UI.Xaml.Controls.ContentDialog sender, Microsoft.UI.Xaml.Controls.ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true; // keep the dialog open
        var package = new DataPackage();
        package.SetText(BuildReportText());
        Clipboard.SetContent(package);
        sender.SecondaryButtonText = "Copied!";
    }

    private void SampleLinkClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url })
        {
            UrlTextBox.Text = url;
            CommitUrl(url);
        }
    }

    private async void SeeReportClick(object sender, RoutedEventArgs e)
    {
        if (_isReportOpen)
        {
            return;
        }
        _isReportOpen = true;
        try
        {
            ReportDialog.SecondaryButtonText = "Copy as text";
            ReportDialog.XamlRoot ??= XamlRoot;
            await ReportDialog.ShowAsync();
        }
        finally
        {
            _isReportOpen = false;
        }
    }

    private async void StartCrawling()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        var cts = _cts = new CancellationTokenSource();
        var token = cts.Token;

        ClearResults();

        var url = UriSourceString?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        Sample.IsLoadingBarVisible = true;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var previewer = GetPreviewer();
            _webViewFetcher.LastResult = null;
            if (BypassCache)
            {
                previewer.ClearCache();
            }

            // Run the high-level previewer and the raw crawler side by side.
            var previewTask = previewer.GetPreviewAsync(url, token);
            var crawlerTask = WebCrawler.FromUrlAsync(NormalizeUrl(url), client: null, RetryAsPreviewBot, token);

            var preview = await previewTask;
            var previewElapsed = stopwatch.Elapsed;
            token.ThrowIfCancellationRequested();

            Card = new PreviewCard(preview);
            AddRows(PreviewRows, preview, includeEmpty: true);

            var crawler = await crawlerTask;
            token.ThrowIfCancellationRequested();

            // Same fallback for the raw crawler: reuse the WebView2 result from the previewer, or fetch now
            // (e.g. when a provider answered and the previewer never needed the HTML).
            var fetchedVia = "HttpClient";
            if (crawler.IsBlocked && UseWebViewFallback)
            {
                if (_webViewFetcher.LastResult == null)
                {
                    await _webViewFetcher.FetchHtmlAsync(new Uri(NormalizeUrl(url)), token);
                    token.ThrowIfCancellationRequested();
                }
                if (_webViewFetcher.LastResult is { Success: true, Html: { } html } webResult)
                {
                    crawler = WebCrawler.FromHtml(html, webResult.FinalUrl ?? NormalizeUrl(url));
                    fetchedVia = "WebView2";
                }
            }
            var crawlElapsed = stopwatch.Elapsed;

            CrawlerRows.Add(new PropertyRow("Fetched via", fetchedVia));
            FillCrawlerRows(crawler);
            AddRows(MetaRows, crawler.GetMetaTags());
            MetaHeader = $"Meta tags ({MetaRows.Count})";

            StatusText = string.Join("  ·  ", new[]
            {
                $"Provider: {preview.Provider ?? "—"}",
                $"Preview: {previewElapsed.TotalMilliseconds:N0} ms",
                $"Crawler: {(crawler.StatusCode is { } s ? ((int)s).ToString(CultureInfo.InvariantCulture) : "no response")}"
                    + (crawler.IsBlocked ? $" (blocked: {crawler.BlockReason})" : string.Empty),
                $"Crawl: {crawlElapsed.TotalMilliseconds:N0} ms",
            });
            if (_webViewFetcher.LastResult is { } web)
            {
                StatusText += $"  ·  WebView2: {web.Note} ({web.Elapsed.TotalMilliseconds:N0} ms)";
            }

            ImagesHeader = "Images (loading…)";
            var images = await crawler.GetAllImagesAsync(validateImages: ValidateImages, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            foreach (var image in images)
            {
                Images.Add(new ImageItem(image));
            }
            ImagesHeader = $"Images ({Images.Count})";
            StatusText += $"  ·  Total: {stopwatch.Elapsed.TotalMilliseconds:N0} ms";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer request replaced this one.
        }
        catch (Exception ex)
        {
            ErrorText = $"{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            if (ReferenceEquals(_cts, cts))
            {
                Sample.IsLoadingBarVisible = false;
            }
        }
    }

    private void UrlTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            CommitUrl(UrlTextBox.Text);
        }
    }
}