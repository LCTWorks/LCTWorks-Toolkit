using Sentry.Protocol;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace LCTWorks.Services.Telemetry.Sentry
{
    public static class SentryTelemetryService
    {
        private static readonly TimeSpan FlushTime = TimeSpan.FromSeconds(2);
        private static readonly ConcurrentDictionary<string, ISpan> SpansPool = new();

        public static void Flush()
        {
            SentrySdk.Flush(FlushTime);
        }

        public static void Initialize(string sentryDns, string environment, string appName, string version, string cachePath)
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = sentryDns;
                options.Environment = environment;
                options.AutoSessionTracking = true;

                options.TracesSampleRate = 1.0;
                options.StackTraceMode = StackTraceMode.Enhanced;
                options.AttachStacktrace = true;
                options.ProfilesSampleRate = 1.0;
                options.Release = $"{appName}@{version}";//Format recommended by Sentry.
                options.CacheDirectoryPath = cachePath;
                options.InitCacheFlushTimeout = TimeSpan.FromSeconds(1);
            });
        }

        public static Guid ReportUnhandledException(Exception exception, string? attachementPath = null)
        {
            exception.Data[Mechanism.HandledKey] = false;
            exception.Data[Mechanism.MechanismKey] = "Application.UnhandledException";
            var unhandledEvent = new SentryEvent(exception)
            {
                Level = SentryLevel.Fatal,
            };

            unhandledEvent.SetTag("priority", "high");

            var id = SentrySdk.CaptureEvent(unhandledEvent, scope =>
            {
                if (attachementPath != null)
                {
                    scope.AddAttachment(attachementPath);
                }
            });

            SentrySdk.Flush(FlushTime);

            return id;
        }

        #region Transactions

        public static void FinishSpan(string id, SpanStatus status = SpanStatus.Ok, IEnumerable<(string, string)>? data = null)
        {
            if (SpansPool.TryRemove(id, out var span))
            {
                if (data != null)
                {
                    span.SetTags(data.Select(x => new KeyValuePair<string, string>(x.Item1, x.Item2)));
                }
                span.Finish(status);
            }
        }

        public static void StartChild(
            string operation,
            string description,
            string childId,
            string parentId,
            IEnumerable<(string, string)>? data = null,
            SpanStatus? finishStatus = null)
        {
            if (SpansPool.TryGetValue(parentId, out var parent))
            {
                var child = parent.StartChild(operation, description);
                if (data != null)
                {
                    child.SetTags(data.Select(x => new KeyValuePair<string, string>(x.Item1, x.Item2)));
                }
                if (finishStatus != null)
                {
                    child.Finish(finishStatus.Value);
                }
                else
                {
                    SpansPool.TryAdd(childId, child);
                }
            }
        }

        public static void StartTransaction(string id, string name, string operation)
        {
            var transaction = SentrySdk.StartTransaction(name, operation);
            SpansPool.TryAdd(id, transaction);
        }

        #endregion Transactions

        public static void TrackError(Exception exception, IEnumerable<(string, string)>? data = null, string? message = null)
        {
            if (exception != null)
            {
                exception.Data[Mechanism.HandledKey] = true;
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        exception.Data[item.Item1] = item.Item2?.ToString() ?? string.Empty;
                    }
                }

                var sentryEvent = new SentryEvent(exception)
                {
                    Level = SentryLevel.Error,
                    Message = message,
                };
                SentrySdk.CaptureEvent(sentryEvent);
            }
        }

        public static void Trail(
            string message,
            string category,
            SentryTrailType type,
            IDictionary<string, string>? data = null)
            => SentrySdk.AddBreadcrumb(message, category, type.ToString().ToLowerInvariant(), data);
    }
}