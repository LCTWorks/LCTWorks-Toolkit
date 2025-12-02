using LCTWorks.Telemetry;
using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System.Profile;

namespace LCTWorks.WinUI.Helpers;

public static class RuntimePackageHelper
{
    static RuntimePackageHelper()
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

    private static string? environment = null;
    private static bool _isChecked = false;

    public static string Environment
    {
        get => environment ?? (IsDebug() ? "Debug" : "Release");
        set => environment = value;
    }

    public static bool IsAppUpdated { get; private set; } = false;

    public static bool IsFirstRun { get; private set; } = false;

    public static void Check()
    {
        if (_isChecked)
        {
            return;
        }
        RuntimeCheck();
    }

    private static void RuntimeCheck()
    {
        var storedVersion = LocalSettingsHelper.LastOpenedVersion;
        if (storedVersion == null)
        {
            IsFirstRun = true;
        }
        var currentAppVersion = Package.Current.Id.Version;
        var lastOpenedVersion = ToPackageVersion(storedVersion);
        if (lastOpenedVersion == null || ComparePackageVersions(currentAppVersion, lastOpenedVersion.Value) > 0) //new or first version
        {
            LocalSettingsHelper.LastOpenedVersion = ToStringVersion(currentAppVersion);
            if (!IsFirstRun)
            {
                IsAppUpdated = true;
            }
        }
        _isChecked = true;
    }

    private static int ComparePackageVersions(PackageVersion v1, PackageVersion v2)
    {
        if (v1.Major != v2.Major)
        {
            return v1.Major - v2.Major;
        }
        if (v1.Minor != v2.Minor)
        {
            return v1.Minor - v2.Minor;
        }
        if (v1.Build != v2.Build)
        {
            return v1.Build - v2.Build;
        }
        return v1.Revision - v2.Revision;
    }

    private static string ToStringVersion(PackageVersion version)
                    => $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

    public static string GetPackageVersion()
    {
        Version version;

        if (IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    private static PackageVersion? ToPackageVersion(string? version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return null;
        }
        var result = version.Split(".");
        return new PackageVersion(
            Convert.ToUInt16(result[0]),
            Convert.ToUInt16(result[1]),
            Convert.ToUInt16(result[2]),
            Convert.ToUInt16(result[3]));
    }

    public static string DeviceManufacturer
    {
        get;
    }

    public static string DeviceModel
    {
        get;
    }

    public static string OsArchitecture
    {
        get;
    }

    public static string DeviceFamily
    {
        get;
    }

    public static bool IsMSIX
    {
        get
        {
            var length = 0;

            return GetCurrentPackageFullName(ref length, null) != 15700L;
        }
    }

    public static string PackageName
    {
        get;
    }

    public static string OSVersion
    {
        get;
    }

    public static string OSDetails
    {
        get;
    }

    public static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }

    public static string LocalCachePath => Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;

    public static string PackageVersion
    {
        get;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

    public static TelemetryEnvironmentContextData GetTelemetryContextData()
        => new(PackageName, LocalCachePath, PackageVersion, CultureInfo.CurrentCulture, DeviceFamily, DeviceManufacturer, DeviceModel, OsArchitecture, OSVersion);
}