using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LCTWorks.Core.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Extensions;

public static class WebView2Extensions
{
    public static async Task<string?> GetHtmlAsync(this WebView2 webView2)
    {
        if (webView2 == null)
        {
            return default;
        }
        if (webView2.CoreWebView2 == null)
        {
            await webView2.EnsureCoreWebView2Async();
        }
        var result = await webView2.CoreWebView2?.ExecuteScriptAsync("document.documentElement.outerHTML;");
        if (!string.IsNullOrEmpty(result))
        {
            return Json.ToObject<string>(result);
        }
        return default;
    }
}