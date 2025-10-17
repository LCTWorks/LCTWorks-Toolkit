using LCTWorks.WinUI.Extensions;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace LCTWorks.WinUI.Helpers;

public static class EngagementHelper
{
    private static StoreContext? _context;

    public static async Task<TimeSpan?> GetRemainingTrialTimeAsync()
    {
        var context = GetStoreContext();
        if (context != null)
        {
            try
            {
                var appLicense = await _context?.GetAppLicenseAsync();

                return appLicense.IsActive && appLicense.IsTrial
                    ? DateTimeOffset.Now - appLicense.ExpirationDate
                    : null;
            }
            catch
            {
            }
        }
        return default;
    }

    public static async Task LaunchEmailAsync(string email, string subject, string body = "")
    {
        var scapedSubject = Uri.EscapeDataString(subject);
        var scapedBody = Uri.EscapeDataString(body);
        var content = $"mailto:?to={email}&subject={scapedSubject}&body={scapedBody}";
        await Windows.System.Launcher.LaunchUriAsync(new Uri(content));
    }

    public static async Task<StoreRateAndReviewStatus> LaunchRateAndReviewAsync()
    {
        var context = GetStoreContext();
        if (context != null)
        {
            try
            {
                var result = await context.RequestRateAndReviewAppAsync();
                return result?.Status ?? StoreRateAndReviewStatus.Error;
            }
            catch (Exception)
            {
            }
        }
        return StoreRateAndReviewStatus.Error;
    }

    private static StoreContext? GetStoreContext()
    {
        if (_context != null)
        {
            return _context;
        }
        try
        {
            var exApp = Application.Current.AsAppExtended();
            if (exApp == null || exApp.MainWindow == null)
            {
                return null;
            }
            //This here, throws a Win32 Unknown exception. No idea why.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(exApp.MainWindow);
            var storeContext = StoreContext.GetDefault();
            WinRT.Interop.InitializeWithWindow.Initialize(storeContext, hWnd);
            return _context = storeContext;
        }
        catch (Exception)
        {
            return default;
        }
    }
}