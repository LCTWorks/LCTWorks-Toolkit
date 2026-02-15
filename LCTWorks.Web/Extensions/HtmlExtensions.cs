using HtmlAgilityPack;
using LCTWorks.Web.Internal;
using System.Net.Http.Headers;
using System.Web;

namespace LCTWorks.Web.Extensions;

public static class HtmlExtensions
{
    #region Html5 entities table

    private static readonly Dictionary<string, string> Html5EntitiesTable = new()
    {
        {"&quot;", "\""},
        {"&num;", "#"},
        {"&dollar;", "$"},
        {"&percnt;", "%"},
        {"&amp;", "&"},
        {"&apos;", "'"},
        {"&lpar;", "("},
        {"&rpar;", ")"},
        {"&ast;", "*"},
        {"&plus;", "+"},
        {"&comma;", ","},
        {"&period;", "."},
        {"&sol;", "/"},
        {"&colon;", ":"},
        {"&semi;", ";"},
        {"&lt;", "<"},
        {"&equals;", "="},
        {"&gt;", ">"},
        {"&quest;", "?"},
        {"&commat;", "@"},
        {"&lsqb;", "["},
        {"&bsol;", "\\"},
        {"&rsqb;", "]"},
        {"&Hat;", "^"},
        {"&lowbar;", "_"},
        {"&grave;", "`"},
        {"&lcub;", "{"},
        {"&verbar;", "|"},
        {"&rcub;", "}"},
        {"&tilde;", "˜"},
        {"&nbsp;", " "},
        {"&iexcl;", "¡"},
        {"&cent;", "¢"},
        {"&pound;", "£"},
        {"&curren;", "¤"},
        {"&yen;", "¥"},
        {"&#x20B9;", "₹"},
        {"&brvbar;", "¦"},
        {"&sect;", "§"},
        {"&uml;", "¨"},
        {"&copy;", "©"},
        {"&ordf;", "ª"},
        {"&laquo;", "«"},
        {"&not;", "¬"},
        {"&shy;", ""},
        {"&reg;", "®"},
        {"&macr;", "¯"},
        {"&deg;", "°"},
        {"&plusmn;", "±"},
        {"&sup2;", "²"},
        {"&sup3;", "³"},
        {"&acute;", "´"},
        {"&micro;", "µ"},
        {"&para;", "¶"},
        {"&middot;", "·"},
        {"&cedil;", "¸"},
        {"&sup1;", "¹"},
        {"&ordm;", "º"},
        {"&raquo;", "»"},
        {"&frac14;", "¼"},
        {"&frac12;", "½"},
        {"&frac34;", "¾"},
        {"&iquest;", "¿"},
        {"&Agrave;", "À"},
        {"&Aacute;", "Á"},
        {"&Acirc;", "Â"},
        {"&Atilde;", "Ã"},
        {"&Auml;", "Ä"},
        {"&Aring;", "Å"},
        {"&AElig;", "Æ"},
        {"&Ccedil;", "Ç"},
        {"&Egrave;", "È"},
        {"&Eacute;", "É"},
        {"&Ecirc;", "Ê"},
        {"&Euml;", "Ë"},
        {"&Igrave;", "Ì"},
        {"&Iacute;", "Í"},
        {"&Icirc;", "Î"},
        {"&Iuml;", "Ï"},
        {"&ETH;", "Ð"},
        {"&Ntilde;", "Ñ"},
        {"&Ograve;", "Ò"},
        {"&Oacute;", "Ó"},
        {"&Ocirc;", "Ô"},
        {"&Otilde;", "Õ"},
        {"&Ouml;", "Ö"},
        {"&times;", "×"},
        {"&Oslash;", "Ø"},
        {"&Ugrave;", "Ù"},
        {"&Uacute;", "Ú"},
        {"&Ucirc;", "Û"},
        {"&Uuml;", "Ü"},
        {"&Yacute;", "Ý"},
        {"&THORN;", "Þ"},
        {"&szlig;", "ß"},
        {"&agrave;", "à"},
        {"&aacute;", "á"},
        {"&acirc;", "â"},
        {"&atilde;", "ã"},
        {"&auml;", "ä"},
        {"&aring;", "å"},
        {"&aelig;", "æ"},
        {"&ccedil;", "ç"},
        {"&egrave;", "è"},
        {"&eacute;", "é"},
        {"&ecirc;", "ê"},
        {"&euml;", "ë"},
        {"&igrave;", "ì"},
        {"&iacute;", "í"},
        {"&icirc;", "î"},
        {"&iuml;", "ï"},
        {"&eth;", "ð"},
        {"&ntilde;", "ñ"},
        {"&ograve;", "ò"},
        {"&oacute;", "ó"},
        {"&ocirc;", "ô"},
        {"&otilde;", "õ"},
        {"&ouml;", "ö"},
        {"&divide;", "÷"},
        {"&oslash;", "ø"},
        {"&ugrave;", "ù"},
        {"&uacute;", "ú"},
        {"&ucirc;", "û"},
        {"&uuml;", "ü"},
        {"&yacute;", "ý"},
        {"&thorn;", "þ"},
        {"&yuml;", "ÿ"},
        {"&OElig;", "Œ"},
        {"&oelig;", "œ"},
        {"&Scaron;", "Š"},
        {"&scaron;", "š"},
        {"&Yuml;", "Ÿ"},
        {"&fnof;", "ƒ"},
        {"&circ;", "ˆ"},
        {"&Alpha;", "Α"},
        {"&Beta;", "Β"},
        {"&Gamma;", "Γ"},
        {"&Delta;", "Δ"},
        {"&Epsilon;", "Ε"},
        {"&Zeta;", "Ζ"},
        {"&Eta;", "Η"},
        {"&Theta;", "Θ"},
        {"&Iota;", "Ι"},
        {"&Kappa;", "Κ"},
        {"&Lambda;", "Λ"},
        {"&Mu;", "Μ"},
        {"&Nu;", "Ν"},
        {"&Xi;", "Ξ"},
        {"&Omicron;", "Ο"},
        {"&Pi;", "Π"},
        {"&Rho;", "Ρ"},
        {"&Sigma;", "Σ"},
        {"&Tau;", "Τ"},
        {"&Upsilon;", "Υ"},
        {"&Phi;", "Φ"},
        {"&Chi;", "Χ"},
        {"&Psi;", "Ψ"},
        {"&Omega;", "Ω"},
        {"&alpha;", "α"},
        {"&beta;", "β"},
        {"&gamma;", "γ"},
        {"&delta;", "δ"},
        {"&epsilon;", "ε"},
        {"&zeta;", "ζ"},
        {"&eta;", "η"},
        {"&theta;", "θ"},
        {"&iota;", "ι"},
        {"&kappa;", "κ"},
        {"&lambda;", "λ"},
        {"&mu;", "μ"},
        {"&nu;", "ν"},
        {"&xi;", "ξ"},
        {"&omicron;", "ο"},
        {"&pi;", "π"},
        {"&rho;", "ρ"},
        {"&sigmaf;", "ς"},
        {"&sigma;", "σ"},
        {"&tau;", "τ"},
        {"&upsilon;", "υ"},
        {"&phi;", "φ"},
        {"&chi;", "χ"},
        {"&psi;", "ψ"},
        {"&omega;", "ω"},
        {"&thetasym;", "ϑ"},
        {"&upsih;", "ϒ"},
        {"&piv;", "ϖ"},
        {"&ensp;", " "},
        {"&emsp;", " "},
        {"&thinsp;", " "},
        {"&zwnj;", ""},
        {"&zwj;", ""},
        {"&lrm;", ""},
        {"&rlm;", ""},
        {"&ndash;", "–"},
        {"&mdash;", "—"},
        {"&lsquo;", "‘"},
        {"&rsquo;", "’"},
        {"&sbquo;", "‚"},
        {"&ldquo;", "“"},
        {"&rdquo;", "”"},
        {"&bdquo;", "„"},
        {"&dagger;", "†"},
        {"&Dagger;", "‡"},
        {"&permil;", "‰"},
        {"&lsaquo;", "‹"},
        {"&rsaquo;", "›"},
        {"&bull;", "•"},
        {"&hellip;", "…"},
        {"&prime;", "′"},
        {"&Prime;", "″"},
        {"&oline;", "‾"},
        {"&frasl;", "⁄"},
        {"&weierp;", "℘"},
        {"&image;", "ℑ"},
        {"&real;", "ℜ"},
        {"&trade;", "™"},
        {"&alefsym;", "ℵ"},
        {"&larr;", "←"},
        {"&uarr;", "↑"},
        {"&rarr;", "→"},
        {"&darr;", "↓"},
        {"&harr;", "↔"},
        {"&crarr;", "↵"},
        {"&lArr;", "⇐"},
        {"&uArr;", "⇑"},
        {"&rArr;", "⇒"},
        {"&dArr;", "⇓"},
        {"&hArr;", "⇔"},
        {"&forall;", "∀"},
        {"&part;", "∂"},
        {"&exist;", "∃"},
        {"&empty;", "∅"},
        {"&nabla;", "∇"},
        {"&isin;", "∈"},
        {"&notin;", "∉"},
        {"&ni;", "∋"},
        {"&prod;", "∏"},
        {"&sum;", "∑"},
        {"&minus;", "−"},
        {"&lowast;", "∗"},
        {"&radic;", "√"},
        {"&prop;", "∝"},
        {"&infin;", "∞"},
        {"&ang;", "∠"},
        {"&and;", "∧"},
        {"&or;", "∨"},
        {"&cap;", "∩"},
        {"&cup;", "∪"},
        {"&int;", "∫"},
        {"&there4;", "∴"},
        {"&sim;", "∼"},
        {"&cong;", "≅"},
        {"&asymp;", "≈"},
        {"&ne;", "≠"},
        {"&equiv;", "≡"},
        {"&le;", "≤"},
        {"&ge;", "≥"},
        {"&sub;", "⊂"},
        {"&sup;", "⊃"},
        {"&nsub;", "⊄"},
        {"&sube;", "⊆"},
        {"&supe;", "⊇"},
        {"&oplus;", "⊕"},
        {"&otimes;", "⊗"},
        {"&perp;", "⊥"},
        {"&sdot;", "⋅"},
        {"&lceil;", "⌈"},
        {"&rceil;", "⌉"},
        {"&lfloor;", "⌊"},
        {"&rfloor;", "⌋"},
        {"&lang;", "〈"},
        {"&rang;", "〉"},
        {"&loz;", "◊"},
        {"&spades;", "♠"},
        {"&clubs;", "♣"},
        {"&hearts;", "♥"},
        {"&diams;", "♦"}
    };

    #endregion Html5 entities table

    /// <summary>
    /// Adds common headers to the HttpRequestHeaders.
    /// </summary>
    public static void AddBrowserHeaders(this HttpRequestHeaders headers)
    {
        if (headers == null)
        {
            return;
        }
        headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36 Edg/144.0.0.0");
        headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        headers.Add("Accept-Language", "en-US,en;q=0.9");
        headers.Add("Upgrade-Insecure-Requests", "1");
    }

    /// <summary>
    /// Decodes HTML5 named character entities in the specified string.
    /// </summary>
    /// <param name="html">The HTML string containing entities to decode.</param>
    /// <returns>
    /// A string with all HTML5 named character entities replaced by their corresponding characters.
    /// Returns the original string if it is null or whitespace.
    /// </returns>
    /// <remarks>
    /// This method replaces HTML5 named entities (e.g., &amp;quot;, &amp;amp;, &amp;lt;) with their
    /// corresponding characters. It does not decode numeric character references (e.g., &amp;#60; or &amp;#x3C;).
    /// For full HTML decoding including numeric references, consider using <see cref="System.Web.HttpUtility.HtmlDecode"/>.
    /// </remarks>
    public static string DecodeHtml5Entities(this string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }
        foreach (var item in Html5EntitiesTable)
        {
            html = html.Replace(item.Key, item.Value);
        }
        return html;
    }

    public static async Task<string?> GetFirstImageFromHtmlCrawlAsync(this HtmlNode? rootNode)
    {
        if (rootNode == null)
        {
            return null;
        }

        var allImgs = rootNode.Descendants("img")
                .OrderByDescending(e => e.GetAttributeValue("width", 0))
                .Select(e => e.GetAttributeValue("src", string.Empty))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

        if (allImgs != null && allImgs.Count != 0)
        {
            var excludingVectors = allImgs.Where(x => !x.EndsWith(".svg") && !x.EndsWith(".ico") && !x.StartsWith("data:image/"));

            foreach (var item in excludingVectors)
            {
                var formattedItem = (item.StartsWith("//")) ? item[2..] : item;
                var uri = new UriString(formattedItem);

                if (uri.IsValid)
                {
                    if (await uri.ValidateImageDataAsync())
                    {
                        return new(uri.Value);
                    }
                }
            }

            var lastAttempt = excludingVectors.Any()
                ? excludingVectors.First()
                : allImgs.FirstOrDefault();

            if (await lastAttempt.IsValidImageDataUrlAsync())
            {
                return new(lastAttempt);
            }
        }
        return null;
    }

    public static async Task<bool> IsValidImageDataUrlAsync(this string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            var response = await HttpTools.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                //Magic bytes check
                using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                var buffer = new byte[12];
                var bytesRead = stream.ReadAsync(buffer, 0, buffer.Length).GetAwaiter().GetResult();

                if (bytesRead >= 2)
                {
                    // PNG: 89 50 4E 47
                    if (buffer[0] == 0x89 && buffer[1] == 0x50 && bytesRead >= 4 && buffer[2] == 0x4E && buffer[3] == 0x47)
                    {
                        return true;
                    }
                    // JPEG: FF D8 FF
                    if (buffer[0] == 0xFF && buffer[1] == 0xD8 && bytesRead >= 3 && buffer[2] == 0xFF)
                    {
                        return true;
                    }
                    // GIF: 47 49 46
                    if (buffer[0] == 0x47 && buffer[1] == 0x49 && bytesRead >= 3 && buffer[2] == 0x46)
                    {
                        return true;
                    }
                    // BMP: 42 4D
                    if (buffer[0] == 0x42 && buffer[1] == 0x4D)
                    {
                        return true;
                    }
                    // WebP: 52 49 46 46 ... 57 45 42 50
                    if (bytesRead >= 12 && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
                        buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                    {
                        return true;
                    }
                    // ICO: 00 00 01 00
                    if (bytesRead >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0x01 && buffer[3] == 0x00)
                    {
                        return true;
                    }
                    // TIFF: 49 49 2A 00 (little-endian) or 4D 4D 00 2A (big-endian)
                    if (bytesRead >= 4 &&
                        ((buffer[0] == 0x49 && buffer[1] == 0x49 && buffer[2] == 0x2A && buffer[3] == 0x00) ||
                         (buffer[0] == 0x4D && buffer[1] == 0x4D && buffer[2] == 0x00 && buffer[3] == 0x2A)))
                    {
                        return true;
                    }
                    // SVG: < or <? (XML-based, starts with < character)
                    if (buffer[0] == 0x3C)
                    {
                        return contentType == "image/svg+xml";
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public static void TryAddRefererHeader(this HttpRequestHeaders headers, string? referer)
    {
        try
        {
            if (referer != null && Uri.TryCreate(referer, UriKind.RelativeOrAbsolute, out var result) && result != null)
            {
                headers.Referrer = new Uri(result.GetLeftPart(UriPartial.Authority));
            }
        }
        catch
        {
        }
    }
}