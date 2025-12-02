using LCTWorks.Core.Extensions;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;

namespace LCTWorks.Telemetry;

public static class TelemetryExtensions
{
    #region Tracing

    public static DisposableTrace StartDisposableTrace(this ITelemetryService service, string id, string name, string operation, string? parentId = null, IEnumerable<(string, string)>? data = null, bool finish = false)
    {
        service.StartTrace(id, name, operation, parentId, data, finish);
        return new DisposableTrace(id, (e) => service.FinishTrace(id, TelemetryTraceStatus.Ok, e));
    }

    #endregion Tracing

    public static void LogAndTrackError(this ITelemetryService service,
        Type callerType,
        Exception exception,
        IEnumerable<(string, string)>? data = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
        => service.LogAndTrackException(exception, LogLevel.Error, callerType, data, callerMember, callerPath, lineNumber);

    public static void LogAndTrackWarning(this ITelemetryService service,
        Type callerType,
        Exception exception,
        IEnumerable<(string, string)>? data = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
        => service.LogAndTrackException(exception, LogLevel.Warning, callerType, data, callerMember, callerPath, lineNumber);

    public static void LogDebug(this ITelemetryService service,
        Type? callerType = null,
        Exception? exception = null,
        TelemetryLogType logType = TelemetryLogType.Default,
        string? message = null,
        IEnumerable<(string, string)>? data = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
        => service.Log(
            message,
            LogLevel.Debug,
            exception,
            null,
            logType.ToLowerInvariantString(),
            callerType,
            data,
            callerMember,
            callerPath,
            lineNumber);

    public static void LogHttpCall(
        this ITelemetryService service,
        Type? callerType = null,
        string? message = null,
        string? header = null,
        LogLevel logLevel = LogLevel.Information,
        IEnumerable<(string, string)>? data = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
        => service.Log(
            message,
            logLevel,
            null,
            header,
            TelemetryLogType.Http.ToLowerInvariantString(),
            callerType,
            data,
            callerMember,
            callerPath,
            lineNumber);

    public static void LogInformation(this ITelemetryService service,
        Type? callerType = null,
        TelemetryLogType logType = TelemetryLogType.Default,
        string? message = null,
        IEnumerable<(string, string)>? data = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
        => service.Log(
            message,
            LogLevel.Information,
            null,
            null,
            logType.ToLowerInvariantString(),
            callerType,
            data,
            callerMember,
            callerPath,
            lineNumber);

    public static void LogNavigation(this ITelemetryService service,
        string? from = null,
        string? to = null,
        Type? callerType = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var category = "Navigation";
        var messageBuilder = new StringBuilder();
        if (callerType != null)
        {
            messageBuilder.Append($"{callerType?.Name}.");
        }
        messageBuilder.Append($"{callerMember}: ");
        if (!string.IsNullOrEmpty(from))
        {
            messageBuilder.Append($"from '{from}' ");
        }
        if (!string.IsNullOrEmpty(to))
        {
            messageBuilder.Append($"to '{to}'");
        }

        service.Log(messageBuilder.ToString(), LogLevel.Information, null, category, TelemetryLogType.Navigation.ToLowerInvariantString(), callerType, null, callerMember, callerPath, lineNumber);
    }

    public static void LogSession(this ITelemetryService service,
        Type? callerType = null,
        string? message = null,
        IEnumerable<(string, string)>? data = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
        => service.Log(
            message,
            LogLevel.Information,
            null,
            "Session log",
            TelemetryLogType.Session.ToLowerInvariantString(),
            callerType,
            data,
            callerMember,
            callerPath,
            lineNumber);

    public static void LogUI(
        this ITelemetryService service,
        Type? callerType = null,
        string? message = null,
        string? header = null,
        LogLevel logLevel = LogLevel.Information,
        IEnumerable<(string, string)>? data = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
        => service.Log(
            message,
            logLevel,
            null,
            header,
            TelemetryLogType.UI.ToLowerInvariantString(),
            callerType,
            data,
            callerMember,
            callerPath,
            lineNumber);

    private static void LogAndTrackException(this ITelemetryService service,
        Exception exception,
        LogLevel logLevel,
        Type? callerType = null,
        IEnumerable<(string, string)>? data = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string type = (logLevel == LogLevel.Warning
            || logLevel == LogLevel.Error
            || logLevel == LogLevel.Critical
            ? TelemetryLogType.Error : TelemetryLogType.Default).ToLowerInvariantString();
        service.Log(null, logLevel, exception, null, type, callerType, data, callerMember, callerPath, lineNumber);
        service.TrackError(exception, data);
    }
}