using System.Text.RegularExpressions;
using System.Web;

namespace LCTWorks.Web.Internal.Previews;

/// <summary>
/// YouTube previews without scraping: thumbnails come straight from i.ytimg.com and
/// title/channel from YouTube's public oEmbed endpoint. Avoids the EU consent wall and bot checks.
/// </summary>
internal sealed partial class YouTubePreviewProvider : IPreviewProvider
{
    public bool IsAuthoritative => false;

    public string Name => "YouTube";

    /// <summary>
    /// Extracts the 11-character video id from watch, youtu.be, shorts, embed, live and /v/ URLs.
    /// </summary>
    public static string? GetVideoId(Uri uri)
    {
        var host = PreviewHttp.NormalizeHost(uri.Host);
        string? candidate;
        if (host == "youtu.be")
        {
            candidate = uri.AbsolutePath.Trim('/').Split('/')[0];
        }
        else
        {
            candidate = HttpUtility.ParseQueryString(uri.Query)["v"];
            if (string.IsNullOrEmpty(candidate))
            {
                var match = PathIdRegex().Match(uri.AbsolutePath);
                candidate = match.Success ? match.Groups[1].Value : null;
            }
        }

        return candidate != null && VideoIdRegex().IsMatch(candidate) ? candidate : null;
    }

    public bool CanHandle(Uri uri)
    {
        var host = PreviewHttp.NormalizeHost(uri.Host);
        return host is "youtube.com" or "music.youtube.com" or "youtu.be" or "youtube-nocookie.com";
    }

    public async Task<WebPreview?> GetPreviewAsync(Uri uri, HttpClient client, CancellationToken cancellationToken)
    {
        var videoId = GetVideoId(uri);
        var canonicalUrl = videoId != null ? $"https://www.youtube.com/watch?v={videoId}" : uri.AbsoluteUri;

        // Works for videos, shorts and playlists.
        var oembedTask = OEmbedResult.FetchAsync(
            client,
            $"https://www.youtube.com/oembed?format=json&url={Uri.EscapeDataString(canonicalUrl)}",
            cancellationToken);

        string? image = null;
        if (videoId != null)
        {
            // maxresdefault only exists for HD uploads (otherwise 404 + a 120x90 placeholder); hqdefault always exists.
            var maxRes = $"https://i.ytimg.com/vi/{videoId}/maxresdefault.jpg";
            image = await PreviewHttp.IsRealImageAsync(client, maxRes, minBytes: 2048, cancellationToken).ConfigureAwait(false)
                ? maxRes
                : $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";
        }

        var oembed = await oembedTask.ConfigureAwait(false);
        if (oembed == null && videoId == null)
        {
            return null;
        }

        return new WebPreview
        {
            Url = uri.AbsoluteUri,
            FinalUrl = canonicalUrl,
            Title = oembed?.Title,
            Author = oembed?.AuthorName,
            SiteName = "YouTube",
            ImageUrl = image ?? oembed?.ThumbnailUrl,
            IconUrl = "https://www.youtube.com/favicon.ico",
            VideoUrl = videoId != null ? $"https://www.youtube.com/embed/{videoId}" : null,
            Provider = Name,
        };
    }

    [GeneratedRegex(@"^/(?:shorts|embed|live|v|e)/([A-Za-z0-9_-]{11})", RegexOptions.IgnoreCase)]
    private static partial Regex PathIdRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{11}$")]
    private static partial Regex VideoIdRegex();
}