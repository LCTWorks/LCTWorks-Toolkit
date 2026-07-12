using LCTWorks.Core.Extensions;
using Microsoft.Extensions.Logging;
using Sentry.Protocol;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace LCTWorks.Telemetry;

internal class SentryTelemetryServiceInternal : ITelemetryService
{
    private const double _debugProfilesSampleRate = 1.0;
    private const double _debugTracesSampleRate = 1.0;
    private const double _productionProfilesSampleRate = 0.2;
    private const double _productionTracesSampleRate = 0.2;
    private static readonly TimeSpan _flushTime = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan _maxSpanAge = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, ISpan> _spansPool = new();

    public bool IncludeSerilogIntegration
    {
        get;
        set;
    }

    public bool IncludeStructuredLogs
    {
        get;
        set;
    }

    public void AppendToTrace(string id, IEnumerable<(string Key, string Value)> data)
    {
        if (_spansPool.TryGetValue(id, out var span))
        {
            span.SetTags(data.Select(x => new KeyValuePair<string, string>(x.Key, x.Value)));
        }
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

    public virtual void Flush()
    {
        SweepStaleSpans();
        SentrySdk.Flush(_flushTime);
        if (IncludeSerilogIntegration)
        {
            Serilog.Log.CloseAndFlush();
        }
    }

    public void Initialize(
        string sentryDsn,
        string? projectName,
        string? environment,
        bool isDebug,
        TelemetryEnvironmentContextData? contextData = null)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = sentryDsn;
            options.Environment = environment;
            options.Debug = isDebug;

            options.TracesSampleRate = isDebug ? _debugTracesSampleRate : _productionTracesSampleRate;
            options.ProfilesSampleRate = isDebug ? _debugProfilesSampleRate : _productionProfilesSampleRate;
            options.AddProfilingIntegration();

            options.EnableLogs = true;

            options.IsGlobalModeEnabled = true;
            options.AutoSessionTracking = true;
            options.StackTraceMode = StackTraceMode.Original;
            options.AttachStacktrace = true;
            options.InitCacheFlushTimeout = TimeSpan.FromSeconds(1);

            if (contextData != null)
            {
                var prefix = !string.IsNullOrWhiteSpace(projectName) ? $"{projectName}@" : string.Empty;
                options.Release = $"{prefix}{contextData.AppVersion}";
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

        IncludeStructuredLogs = true;

        SentrySdk.ConfigureScope(scope =>
        {
            scope.User = new SentryUser
            {
                Id = GetOrCreateInstallationId(contextData?.AppLocalCachePath),
            };

            if (contextData != null)
            {
                scope.Contexts.OperatingSystem.Name = contextData.OsName;
                scope.Contexts.OperatingSystem.Version = contextData.OsVersion;
                scope.Contexts.Device.Architecture = contextData.OsArchitecture;
                scope.Contexts.Device.DeviceType = contextData.DeviceFamily;
                scope.Contexts.Device.Model = contextData.DeviceModel;
                scope.Contexts.Device.Manufacturer = contextData.DeviceManufacturer;
            }
        });
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

        LogStructured(level, message, breadcrumbCategory, type, tags);
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
            BreadcrumbLevel.Fatal);

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
                    TelemetryTraceStatus.InternalError => SpanStatus.InternalError,
                    _ => SpanStatus.UnknownError,
                };

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

    /// <summary>
    /// Returns a stable, anonymized per-installation identifier, persisted next to the
    /// Sentry cache. Falls back to a transient id if no writable cache path is available.
    /// </summary>
    private static string GetOrCreateInstallationId(string? cachePath)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(cachePath))
            {
                Directory.CreateDirectory(cachePath);
                var file = Path.Combine(cachePath, "installation-id");
                if (File.Exists(file))
                {
                    var existing = File.ReadAllText(file).Trim();
                    if (Guid.TryParse(existing, out _))
                    {
                        return existing;
                    }
                }

                var id = Guid.NewGuid().ToString();
                File.WriteAllText(file, id);
                return id;
            }
        }
        catch
        {
        }

        return Guid.NewGuid().ToString();
    }

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
            LogLevel.Critical => BreadcrumbLevel.Fatal,
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

    private void LogStructured(
        LogLevel level,
        string message,
        string? category,
        string? type,
        IEnumerable<(string Key, string Value)>? tags)
    {
        if (!IncludeStructuredLogs || level == LogLevel.None)
        {
            return;
        }

        void Configure(SentryLog log)
        {
            if (!string.IsNullOrEmpty(category))
            {
                log.SetAttribute("category", category);
            }
            if (!string.IsNullOrEmpty(type))
            {
                log.SetAttribute("type", type);
            }
            if (tags != null)
            {
                foreach (var (Key, Value) in tags)
                {
                    if (!string.IsNullOrEmpty(Key) && Value != null)
                    {
                        log.SetAttribute(Key, Value);
                    }
                }
            }
        }

        switch (level)
        {
            case LogLevel.Trace:
                SentrySdk.Logger.LogTrace(Configure, message);
                break;

            case LogLevel.Debug:
                SentrySdk.Logger.LogDebug(Configure, message);
                break;

            case LogLevel.Information:
                SentrySdk.Logger.LogInfo(Configure, message);
                break;

            case LogLevel.Warning:
                SentrySdk.Logger.LogWarning(Configure, message);
                break;

            case LogLevel.Error:
                SentrySdk.Logger.LogError(Configure, message);
                break;

            case LogLevel.Critical:
                SentrySdk.Logger.LogFatal(Configure, message);
                break;
        }
    }

    private void SweepStaleSpans()
    {
        if (_spansPool.IsEmpty)
        {
            return;
        }

        var cutoff = DateTimeOffset.UtcNow - _maxSpanAge;
        foreach (var pair in _spansPool)
        {
            if (pair.Value.StartTimestamp <= cutoff
                && _spansPool.TryRemove(pair.Key, out var span))
            {
                span.Finish(SpanStatus.DeadlineExceeded);
            }
        }
    }

    #endregion Private
}