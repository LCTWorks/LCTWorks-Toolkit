using System.Globalization;

namespace LCTWorks.Common.Services.Telemetry;

public record TelemetryEnvironmentContextData(
    string? AppDisplayName,
    string? AppLocalCachePath,
    string? AppTheme,
    string? AppVersion,
    CultureInfo? Culture,
    string? DeviceFamily,
    string? DeviceManufacturer,
    string? DeviceModel,
    string? OsArchitecture,
    string? OsName,
    string? OsVersion);