using System.Globalization;

namespace LCTWorks.Telemetry;

public record TelemetryEnvironmentContextData(
    string? AppDisplayName,
    string? AppLocalCachePath = null,
    string? AppVersion = null,
    CultureInfo? Culture = null,
    string? DeviceFamily = null,
    string? DeviceManufacturer = null,
    string? DeviceModel = null,
    string? OsArchitecture = null,
    string? OsName = null,
    string? OsVersion = null);