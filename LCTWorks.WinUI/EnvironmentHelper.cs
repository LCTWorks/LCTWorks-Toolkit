using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using Windows.ApplicationModel;
using System.Runtime.InteropServices;
using System.Text;
using Windows.System.Profile;
using Windows.Security.ExchangeActiveSyncProvisioning;
using LCTWorks.Common.Services.Telemetry;
using System.Globalization;

namespace LCTWorks.Common.WinUI;

public static class EnvironmentHelper
{
    static EnvironmentHelper()
    {
        var dfVersion = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
        var osMajor = (ushort)((dfVersion & 0xFFFF000000000000L) >> 48);
        var osMinor = (ushort)((dfVersion & 0x0000FFFF00000000L) >> 32);
        var osBuild = (ushort)((dfVersion & 0x00000000FFFF0000L) >> 16);
        var osRevision = (ushort)(dfVersion & 0x000000000000FFFFL);

        Version version;
        if (IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            PackageName = Package.Current.DisplayName;
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
            PackageName = Assembly.GetExecutingAssembly().GetName().Name!;
        }

        var easClientDeviceInformation = new EasClientDeviceInformation();

        DeviceModel = easClientDeviceInformation.SystemProductName;
        DeviceManufacturer = easClientDeviceInformation.SystemManufacturer;
        OsArchitecture = Package.Current.Id.Architecture.ToString();
        DeviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;
        OSVersion = $"{osMajor}.{osMinor}.{osBuild}.{osRevision}";
        OSDetails = $"WINDOWS {OSVersion}";
        PackageVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    public static string DeviceManufacturer { get; }

    public static string DeviceModel { get; }

    public static string OsArchitecture { get; }

    public static string DeviceFamily { get; }

    public static bool IsMSIX
    {
        get
        {
            var length = 0;

            return GetCurrentPackageFullName(ref length, null) != 15700L;
        }
    }

    public static string PackageName { get; }

    public static string OSVersion { get; }

    public static string OSDetails { get; }

    public static bool IsDebug()
    {
#if DEBUG
        return true;
#else
            return false;
#endif
    }

    public static string LocalCachePath
    {
        get
        {
            var localCache = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
            return localCache;
        }
    }

    public static string PackageVersion { get; }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

    public static TelemetryEnvironmentContextData GetTelemetryContextData()
        => new(PackageName, LocalCachePath, PackageVersion, CultureInfo.CurrentCulture, DeviceFamily, DeviceManufacturer, DeviceModel, OsArchitecture, OSVersion);
}