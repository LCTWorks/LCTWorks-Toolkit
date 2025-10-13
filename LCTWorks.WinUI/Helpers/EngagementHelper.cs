using LCTWorks.WinUI.Extensions;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace LCTWorks.WinUI.Helpers;

public static class EngagementHelper
{
    public static async Task LaunchEmailAsync(string email, string subject, string body = "")
    {
        var scapedSubject = Uri.EscapeDataString(subject);
        var scapedBody = Uri.EscapeDataString(body);
        var content = $"mailto:?to={email}&subject={scapedSubject}&body={scapedBody}";
        await Windows.System.Launcher.LaunchUriAsync(new Uri(content));
    }

    public static async Task<StoreRateAndReviewStatus> LaunchRateAndReviewAsync()
    {
        var exApp = Application.Current.AsAppExtended();
        if (exApp == null || exApp.MainWindow == null)
        {
            return StoreRateAndReviewStatus.Error;
        }

        //This here, throws a Win32 Unknown exception. No idea why.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(exApp.MainWindow);
        var storeContext = StoreContext.GetDefault();
        WinRT.Interop.InitializeWithWindow.Initialize(storeContext, hWnd);
        var result = await storeContext.RequestRateAndReviewAppAsync();
        return result?.Status ?? StoreRateAndReviewStatus.Error;
    }
}