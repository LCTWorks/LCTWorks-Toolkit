using Windows.ApplicationModel;

namespace LCTWorks.WinUI.Extensions;

public static class PackageVersionExtensions
{
    /// <summary>
    /// Converts a PackageVersion to a version string.
    /// </summary>
    /// <param name="packageVersion">The PackageVersion to convert.</param>
    /// <param name="parts">The number of parts to include in the version string.</param>
    public static string ToVersionString(this PackageVersion packageVersion, int parts = 4)
    {
        return parts switch
        {
            4 => $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}",
            3 => $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}",
            2 => $"{packageVersion.Major}.{packageVersion.Minor}",
            1 => $"{packageVersion.Major}",
            _ => string.Empty
        };
    }
}