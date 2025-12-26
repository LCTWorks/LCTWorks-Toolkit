using LCTWorks.Core.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;

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

    public static bool HasCertificateErrors(this CoreWebView2NavigationCompletedEventArgs args)
    {
        var errorStatus = args.WebErrorStatus;
        return errorStatus == CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect ||
             errorStatus == CoreWebView2WebErrorStatus.ClientCertificateContainsErrors ||
             errorStatus == CoreWebView2WebErrorStatus.CertificateRevoked ||
             errorStatus == CoreWebView2WebErrorStatus.CertificateIsInvalid ||
             errorStatus == CoreWebView2WebErrorStatus.ServerUnreachable;
    }
}