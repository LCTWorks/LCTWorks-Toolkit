using Microsoft.Extensions.Logging;
using Sentry.Protocol;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace LCTWorks.Common.Services.Telemetry
{
    public class SentryTelemetryServiceInternal : ITelemetryService
    {
        private static readonly TimeSpan _flushTime = TimeSpan.FromSeconds(2);
        private static readonly ConcurrentDictionary<string, ISpan> _spansPool = new ConcurrentDictionary<string, ISpan>();

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

        public void Flush()
        {
            SentrySdk.Flush(_flushTime);
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

        public void Log(
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
            //Log:
            //string path = callerType?.Name ?? Path.GetFileName(callerPath);
            //string logMessage = $"[{path} / {callerMember ?? string.Empty}: {lineNumber}] {message ?? string.Empty}";
            //_logger.Log(level, exception, logMessage);

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
        }

        public Guid ReportUnhandledException(Exception exception)
        {
            var serializedException = SerializeException(exception);

            //Log:
            //_logger.LogCritical(message);
            //_logger.LogCritical(serializedException);

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

            Flush();

            return id;
        }

        public void TrackError(Exception exception, IEnumerable<(string Key, string Value)>? tags = null, string? message = null)
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
                    sentryEvent.SetTags(tags.ToValidStringKeyValuePair());
                }

                SentrySdk.CaptureEvent(sentryEvent);
            }
        }

        #region Traces

        public void AppentToTrace(string id, IEnumerable<(string Key, string Value)> data)
        {
        }

        public void FinishTrace(string id, string? status = null, Exception? exception = null, IEnumerable<(string Key, string Value)>? data = null)
        {
        }

        public void StartTrace(string id, string? parentId = null, IEnumerable<(string Key, string Value)>? data = null, bool finish = false)
        {
        }

        #endregion Traces

        #region Private

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

        #endregion Private
    }
}