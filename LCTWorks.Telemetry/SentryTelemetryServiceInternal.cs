using LCTWorks.Core.Extensions;
using Microsoft.Extensions.Logging;
using Sentry.Protocol;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace LCTWorks.Telemetry;

public class SentryTelemetryServiceInternal : ITelemetryService
{
    private static readonly TimeSpan _flushTime = TimeSpan.FromSeconds(2);
    private static readonly ConcurrentDictionary<string, ISpan> _spansPool = new();

    public bool IncludeSerilogIntegration
    {
        get;
        set;
    }

    public void ConfigureScope(IEnumerable<(string Key, string Value)>? tags = null)
    {
        SentrySdk.ConfigureScope(scope =>
        {
            if (tags != null && tags.Any())
            {
                //Add or remove tags based on value.
                foreach (var (Key, Value) in tags)
                {
                    if (!string.IsNullOrEmpty(Key))
                    {
                        if (string.IsNullOrEmpty(Value))
                        {
                            scope.UnsetTag(Key);
                        }
                        else
                        {
                            scope.SetTag(Key, Value);
                        }
                    }
                }
            }
        });
    }

    public virtual void Flush()
    {
        SentrySdk.Flush(_flushTime);
        if (IncludeSerilogIntegration)
        {
            Serilog.Log.CloseAndFlush();
        }
    }

    public void Initialize(
        string sentryDsn,
        string? environment,
        bool isDebug,
        TelemetryEnvironmentContextData? contextData = null)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = sentryDsn;
            options.Environment = environment;
            options.Debug = isDebug;
            options.TracesSampleRate = 1.0;
            options.IsGlobalModeEnabled = true;
            options.StackTraceMode = StackTraceMode.Original;
            options.AttachStacktrace = true;
            options.InitCacheFlushTimeout = TimeSpan.FromSeconds(1);

            if (contextData != null)
            {
                options.Release = $"{contextData.AppDisplayName}@{contextData.AppVersion}";
                options.CacheDirectoryPath = contextData.AppLocalCachePath;
            }

            options.SetBeforeBreadcrumb(bc =>
            {
                //Filter out auto-breadcrumbs by captured exceptions.
                if (bc.Category == "Exception")
                {
                    return null;
                }
                return bc;
            });
        });

        if (contextData != null)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.Contexts.OperatingSystem.Name = contextData.OsName;
                scope.Contexts.OperatingSystem.Version = contextData.OsVersion;
                scope.Contexts.Device.Architecture = contextData.OsArchitecture;
                scope.Contexts.Device.DeviceType = contextData.DeviceFamily;
                scope.Contexts.Device.Model = contextData.DeviceModel;
                scope.Contexts.Device.Manufacturer = contextData.DeviceManufacturer;
            });
        }
    }

    public virtual void Log(
        string? message = null,
        LogLevel level = LogLevel.Information,
        Exception? exception = null,
        string? category = "",
        string? type = "",
        Type? callerType = null,
        IEnumerable<(string Key, string Value)>? tags = null,
        [CallerMemberName] string callerMember = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        //Breadcrumb:
        if (string.IsNullOrWhiteSpace(message) && exception != null)
        {
            message = $"{exception?.GetType().Name ?? ""}: {exception?.Message}";
        }

        message ??= "Unknown";

        var breadcrumbCategory = category ?? $"{callerType?.Name ?? string.Empty}.{callerMember}";

        SentrySdk.AddBreadcrumb(
            message,
            breadcrumbCategory,
            type ?? TelemetryLogType.Default.ToString(),
            tags?.ToDictionary(),
            ToBreadCrumbLevel(level));

        LogSerilog(level, message);
    }

    public virtual Guid ReportUnhandledException(Exception exception)
    {
        var serializedException = SerializeException(exception);

        //Set breadcrumb with extra info:
        SentrySdk.AddBreadcrumb(
            exception.Message,
            "Unhandled exception info",
            TelemetryLogType.Info.ToLowerInvariantString(),
            new[] { ("exception data", serializedException) }.ToDictionary(),
            BreadcrumbLevel.Critical);

        //Set the critical event:
        exception.Data[Mechanism.HandledKey] = false;
        exception.Data[Mechanism.MechanismKey] = "Application.UnhandledException";
        var unhandledEvent = new SentryEvent(exception)
        {
            Level = SentryLevel.Fatal,
        };

        unhandledEvent.SetTag("priority", "high");

        var id = SentrySdk.CaptureEvent(unhandledEvent);
        LogSerilog(LogLevel.Critical, exception.Message);

        Flush();

        return id;
    }

    public virtual void TrackError(Exception exception, IEnumerable<(string Key, string Value)>? tags = null, string? message = null)
    {
        if (exception != null)
        {
            exception.Data[Mechanism.HandledKey] = true;

            var sentryEvent = new SentryEvent(exception)
            {
                Level = SentryLevel.Error,
                Message = message,
            };

            if (tags != null)
            {
                sentryEvent.SetTags(tags.ValidateStringKeyValuePair());
            }

            SentrySdk.CaptureEvent(sentryEvent);
        }
    }

    #region Traces

    public void AppentToTrace(string id, IEnumerable<(string Key, string Value)> data)
    {
        if (_spansPool.TryGetValue(id, out var span))
        {
            span.SetTags(data.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)));
        }
    }

    public void FinishTrace(string id, TelemetryTraceStatus? status = null, Exception? exception = null, IEnumerable<(string Key, string Value)>? data = null)
    {
        if (_spansPool.TryRemove(id, out var span))
        {
            if (data != null)
            {
                span.SetTags(data.ValidateStringKeyValuePair());
            }

            var activeChildren = _spansPool.Where(pair => pair.Value.ParentSpanId == span.SpanId).ToList();
            foreach (var item in activeChildren)
            {
                _spansPool.TryRemove(item.Key, out var child);
                child?.Finish();
            }

            var finishStatus = status != null ? ConvertStatus(status.Value) : span.Status;

            if (finishStatus != null)
            {
                var transaction = span.GetTransaction();
                if (PropagateStatus(transaction, finishStatus))
                {
                    transaction.Status = finishStatus;
                }
                if (exception != null)
                {
                    span.Finish(exception, finishStatus.Value);
                }
                else
                {
                    span.Finish(finishStatus.Value);
                }
            }
            else
            {
                span.Finish();
            }
        }
    }

    public void StartTrace(string id, string name, string operation, string? parentId = null, IEnumerable<(string Key, string Value)>? data = null, bool finish = false)
    {
        if (parentId == null)
        {
            var transaction = SentrySdk.StartTransaction(name, operation);
            if (data != null)
            {
                transaction.SetTags(data.ValidateStringKeyValuePair());
            }
            _spansPool.TryAdd(id, transaction);
        }
        else
        {
            if (_spansPool.TryGetValue(parentId, out var parent))
            {
                var child = parent.StartChild(operation, name);
                if (data != null)
                {
                    child.SetTags(data.ValidateStringKeyValuePair());
                }
                if (finish)
                {
                    child.Finish();
                }
                else
                {
                    _spansPool.TryAdd(id, child);
                }
            }
        }
    }

    /// <summary>
    /// This status values are not considered errors.
    /// </summary>
    private static bool IsSuccessStatus(SpanStatus? status)
        => status == null
        || status == SpanStatus.Ok
        || status == SpanStatus.UnknownError
        || status == SpanStatus.Cancelled;

    private static bool PropagateStatus(ITransactionTracer transaction, SpanStatus? spanStatus)
    {
        if (transaction == null || spanStatus == null)
        {
            return false;
        }
        if (spanStatus == SpanStatus.Ok)
        {
            return false;
        }
        if (IsSuccessStatus(spanStatus))
        {
            return false;
        }
        return IsSuccessStatus(transaction.Status);
    }

    #endregion Traces

    #region Private

    private static LogEventLevel ConvertLogLevel(LogLevel level)
        => level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information,
        };

    private static SpanStatus ConvertStatus(TelemetryTraceStatus traceState)
            => traceState switch
            {
                TelemetryTraceStatus.Ok => SpanStatus.Ok,
                TelemetryTraceStatus.AuthorizationError => SpanStatus.PermissionDenied,
                TelemetryTraceStatus.InvalidArgument => SpanStatus.InvalidArgument,
                TelemetryTraceStatus.OutOfRange => SpanStatus.OutOfRange,
                TelemetryTraceStatus.Cancelled => SpanStatus.Cancelled,
                TelemetryTraceStatus.UnknownError => SpanStatus.UnknownError,
                _ => SpanStatus.UnknownError,
            };

    private static string SerializeException(Exception exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Exception: {exception.GetType().Name}");
        sb.AppendLine($"Message: {exception.Message}");
        sb.AppendLine($"Stack Trace: {exception.StackTrace}");
        if (exception.InnerException != null)
        {
            sb.AppendLine("Inner Exception:");
            sb.AppendLine(SerializeException(exception.InnerException));
        }
        return sb.ToString();
    }

    private static BreadcrumbLevel ToBreadCrumbLevel(LogLevel level)
        => level switch
        {
            LogLevel.Debug => BreadcrumbLevel.Debug,
            LogLevel.Warning => BreadcrumbLevel.Warning,
            LogLevel.Error => BreadcrumbLevel.Error,
            LogLevel.Critical => BreadcrumbLevel.Critical,
            _ => BreadcrumbLevel.Info,
        };

    private void LogSerilog(LogLevel level, string text)
    {
        var logger = Serilog.Log.Logger;
        if (!IncludeSerilogIntegration || logger == null)
        {
            return;
        }
        var eventLevel = ConvertLogLevel(level);
        logger.Write(eventLevel, text);
    }

    #endregion Private
}