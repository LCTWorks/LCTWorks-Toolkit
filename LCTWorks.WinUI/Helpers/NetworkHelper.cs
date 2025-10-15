using Windows.Networking.Connectivity;

namespace LCTWorks.WinUI.Helpers;

public static class NetworkHelper
{
    public static bool IsInternetAvailable
    {
        get
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            return profile != null && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
        }
    }
}