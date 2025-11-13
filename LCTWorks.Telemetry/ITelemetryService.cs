using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace LCTWorks.Telemetry;

public interface ITelemetryService
{
    void AppentToTrace(string id, IEnumerable<(string Key, string Value)> data);

    void ConfigureScope(IEnumerable<(string Key, string Value)>? tags = null);

    void FinishTrace(string id, TelemetryTraceStatus? status = null, Exception? exception = null, IEnumerable<(string Key, string Value)>? data = null);

    void Flush();

    void Initialize(string serviceKey, string? environment, bool isDebug, TelemetryEnvironmentContextData? contextData = null);

    void Log(string? message = null,
            LogLevel level = LogLevel.Information,
            Exception? exception = null,
            string? category = "",
            string? type = "",
            Type? callerType = null,
            IEnumerable<(string Key, string Value)>? tags = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int lineNumber = 0);

    Guid ReportUnhandledException(Exception exception);

    void StartTrace(string id, string name, string operation, string? parentId = null, IEnumerable<(string Key, string Value)>? data = null, bool finish = false);

    void TrackError(Exception exception, IEnumerable<(string Key, string Value)>? tags = null, string? message = null);
}