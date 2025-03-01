namespace LCTWorks.Common.Services.Telemetry;

public interface ITelemetryService
{
    void AppentToTrace(string id, IEnumerable<(string, string)> data);

    void Configure(string? userId = null, IEnumerable<(string, string)>? data = null);

    void FinishTrace(string id, string? status = null, Exception? exception = null, IEnumerable<(string, string)>? data = null);

    void Flush();

    void Initialize(string serviceKey, string? environment, bool? debug, TelemetryEnvironmentContextData? contextData = null);

    Guid ReportUnhandledException(Exception exception, string? message = null);

    void StartTrace(string id, string? parentId = null, IEnumerable<(string, string)>? data = null, bool finish = false);

    void TrackError(Exception exception, IEnumerable<(string, string)>? data = null, string? message = null);

    void Trail(string message, TrailLevel level = TrailLevel.Information, Exception? exception = null, string category = "", string type = "", IEnumerable<(string, string)>? data = null);
}