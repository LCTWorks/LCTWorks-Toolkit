using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace LCTWorks.Workshop.Services;

/// <summary>
/// Result of the last <see cref="WebView2PageFetcher"/> run, for diagnostics.
/// </summary>
public sealed record WebViewFetchResult(Uri Uri, string? FinalUrl, string? Html, bool Success, string Note, TimeSpan Elapsed);

/// <summary>
/// Loads a page in a real browser (a hidden <see cref="WebView2"/>, i.e. Edge) and returns the rendered HTML.
/// Meant to be plugged into <c>WebPreviewerOptions.BlockedPageFetcher</c>.
/// </summary>
/// <remarks>
/// <para>
/// Because it's a real browser (real TLS/HTTP2 fingerprint, JavaScript, cookies), it passes most automatic,
/// non-interactive bot checks such as Cloudflare's "Just a moment..." page. It does <b>not</b> try to solve
/// interactive challenges (Turnstile/captcha): if the page is still a challenge after <see cref="Timeout"/>, it gives up.
/// </para>
/// <para>
/// The <see cref="WebView2"/> must be in the visual tree (it can be 1x1 and transparent). All WebView2 work is
/// marshalled to its UI thread, and requests are serialized because a single WebView is shared.
/// The clearance cookie is kept in the WebView2 profile, so repeat visits to the same site are faster.
/// </para>
/// </remarks>
public sealed class WebView2PageFetcher
{
    // Returns the page state as a JSON string: readiness, title, final URL and whether it's still a bot challenge.
    private const string StateScript = """
        (() => {
            const t = document.title || '';
            const html = document.documentElement ? document.documentElement.innerHTML : '';
            const challenge =
                /^Just a moment/i.test(t) ||
                /Attention Required!|Please Wait\.\.\. \| Cloudflare|Checking your browser/i.test(t) ||
                !!document.querySelector('#challenge-form, #challenge-running, #cf-challenge-running, #challenge-stage') ||
                html.indexOf('/cdn-cgi/challenge-platform/h/') >= 0;
            return JSON.stringify({ ready: document.readyState, title: t, url: location.href, challenge: challenge, length: html.length });
        })()
        """;

    private readonly DispatcherQueue _dispatcher;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly WebView2 _webView;
    private bool _initialized;

    public WebView2PageFetcher(WebView2 webView)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _dispatcher = webView.DispatcherQueue;
    }

    /// <summary>
    /// The last fetch performed, or null. Reset it before a run to know whether the fallback was used.
    /// </summary>
    public WebViewFetchResult? LastResult { get; set; }

    /// <summary>
    /// Extra wait after the page is ready, for scripts that inject meta tags (SPAs).
    /// </summary>
    public TimeSpan SettleDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Maximum time to wait for the page (including the automatic challenge) to finish.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Signature-compatible with <c>WebPreviewerOptions.BlockedPageFetcher</c>.
    /// Safe to call from any thread. Returns null on timeout, interactive challenge or error.
    /// </summary>
    public async Task<string?> FetchHtmlAsync(Uri uri, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await RunOnUIThreadAsync(() => FetchOnUIThreadAsync(uri, cancellationToken)).ConfigureAwait(false);
            LastResult = result;
            return result.Success ? result.Html : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// ExecuteScriptAsync returns the script result JSON-encoded; this unwraps a string result.
    /// (JsonDocument instead of JsonSerializer: no reflection, trimming-safe.)
    /// </summary>
    private static string? ParseJsonString(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.String ? doc.RootElement.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PageState? ParseState(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new PageState(
                Ready: root.TryGetProperty("ready", out var ready) ? ready.GetString() : null,
                Url: root.TryGetProperty("url", out var url) ? url.GetString() : null,
                IsChallenge: root.TryGetProperty("challenge", out var challenge) && challenge.ValueKind == JsonValueKind.True,
                Length: root.TryGetProperty("length", out var length) && length.TryGetInt32(out var l) ? l : 0);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized && _webView.CoreWebView2 != null)
        {
            return;
        }

        await _webView.EnsureCoreWebView2Async();
        var core = _webView.CoreWebView2!;

        // Headless-ish: no UI, no popups, no downloads, no sound.
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.IsMuted = true;
        core.NewWindowRequested += (_, e) => e.Handled = true;
        core.DownloadStarting += (_, e) => e.Cancel = true;

        _initialized = true;
    }

    private async Task<WebViewFetchResult> FetchOnUIThreadAsync(Uri uri, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        WebViewFetchResult Result(string? html, bool success, string note, string? finalUrl = null)
            => new(uri, finalUrl, html, success, note, stopwatch.Elapsed);

        try
        {
            await EnsureInitializedAsync();
        }
        catch (Exception ex)
        {
            // e.g. the WebView2 runtime is missing.
            return Result(null, false, $"WebView2 unavailable: {ex.Message}");
        }

        var core = _webView.CoreWebView2!;
        var navigationCompleted = false;
        CoreWebView2WebErrorStatus? networkError = null;

        void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            navigationCompleted = true;
            // HTTP error pages (e.g. Cloudflare's 403/503 challenge) are still rendered; only real network errors stop us.
            if (!args.IsSuccess && args.WebErrorStatus is CoreWebView2WebErrorStatus.CannotConnect
                or CoreWebView2WebErrorStatus.HostNameNotResolved
                or CoreWebView2WebErrorStatus.ConnectionAborted
                or CoreWebView2WebErrorStatus.ConnectionReset
                or CoreWebView2WebErrorStatus.Disconnected
                or CoreWebView2WebErrorStatus.Timeout
                or CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect
                or CoreWebView2WebErrorStatus.CertificateExpired
                or CoreWebView2WebErrorStatus.CertificateIsInvalid
                or CoreWebView2WebErrorStatus.CertificateRevoked)
            {
                networkError = args.WebErrorStatus;
            }
        }

        core.NavigationCompleted += OnNavigationCompleted;
        try
        {
            core.Navigate(uri.AbsoluteUri);

            var sawChallenge = false;
            while (stopwatch.Elapsed < Timeout)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    core.Stop();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Delay(400, CancellationToken.None);

                if (networkError is { } error)
                {
                    return Result(null, false, $"Network error: {error}");
                }
                if (!navigationCompleted)
                {
                    continue;
                }

                var state = ParseState(ParseJsonString(await core.ExecuteScriptAsync(StateScript)));
                if (state == null)
                {
                    continue;
                }
                if (state.IsChallenge)
                {
                    sawChallenge = true;
                    continue; // the challenge script reloads/redirects by itself when it passes
                }
                if (state.Ready != "complete" || state.Length == 0)
                {
                    continue;
                }

                await Task.Delay(SettleDelay, CancellationToken.None);
                var html = ParseJsonString(await core.ExecuteScriptAsync("document.documentElement.outerHTML"));
                return string.IsNullOrWhiteSpace(html)
                    ? Result(null, false, "Empty document", state.Url)
                    : Result(html, true, sawChallenge ? "Passed bot challenge" : "Loaded", state.Url);
            }

            return Result(null, false, "Timed out (likely an interactive challenge)");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result(null, false, $"Error: {ex.Message}");
        }
        finally
        {
            core.NavigationCompleted -= OnNavigationCompleted;
            // Stop scripts/media of the loaded page.
            try
            {
                core.Navigate("about:blank");
            }
            catch
            {
            }
        }
    }

    private Task<T> RunOnUIThreadAsync<T>(Func<Task<T>> action)
    {
        if (_dispatcher.HasThreadAccess)
        {
            return action();
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var enqueued = _dispatcher.TryEnqueue(async () =>
        {
            try
            {
                tcs.SetResult(await action());
            }
            catch (OperationCanceledException ex)
            {
                tcs.SetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        if (!enqueued)
        {
            tcs.SetException(new InvalidOperationException("Could not dispatch to the WebView2 UI thread."));
        }
        return tcs.Task;
    }

    private sealed record PageState(string? Ready, string? Url, bool IsChallenge, int Length);
}
