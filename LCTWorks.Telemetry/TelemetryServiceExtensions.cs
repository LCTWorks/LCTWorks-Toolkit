using LCTWorks.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace LCTWorks.Telemetry;

public static class TelemetryServiceExtensions
{
    public static IServiceCollection AddSentry(this IServiceCollection services)
    {
        return services.AddSingleton<ITelemetryService, SentryTelemetryServiceInternal>();
    }

    /// <summary>
    /// Adds Sentry's <see cref="SentryHttpMessageHandler"/> to an HttpClient so outbound
    /// requests are captured as spans and surface in Insights (Requests dashboard).
    /// Usage: <c>services.AddHttpClient("api").AddSentryTracing();</c>
    /// </summary>
    public static IHttpClientBuilder AddSentryTracing(this IHttpClientBuilder builder)
        => builder.AddHttpMessageHandler(() => new SentryHttpMessageHandler());

    public static IServiceCollection AddSentry(this IServiceCollection services, string? sentryDsn, string? projectName, string? environment, bool isDebug, TelemetryEnvironmentContextData? contextData = null)
    {
        var sentryService = new SentryTelemetryServiceInternal();
        if (!string.IsNullOrWhiteSpace(sentryDsn))
        {
            sentryService.Initialize(sentryDsn, projectName, environment, isDebug, contextData);
            services = services.AddSingleton<ITelemetryService>(sentryService);
        }
        return services;
    }

    public static IServiceCollection AddSentryAndSerilog(this IServiceCollection services, string? sentryDsn, string? projectName, string? environment, bool isDebug, TelemetryEnvironmentContextData contextData)
    {
        bool serilogIncluded = false;
        if (contextData.AppLocalCachePath != null)
        {
            var filePath = Path.Join(contextData.AppLocalCachePath, "Logs");
            services = services.AddSerilog(filePath, LogEventLevel.Information, isDebug, false);
            serilogIncluded = true;
        }

        var sentryService = new SentryTelemetryServiceInternal
        {
            IncludeSerilogIntegration = serilogIncluded
        };
        sentryService.Initialize(sentryDsn ?? string.Empty, projectName, environment, isDebug, contextData);
        services = services.AddSingleton<ITelemetryService>(sentryService);

        return services;
    }

    public static IServiceCollection AddSerilog(this IServiceCollection services,
            string logFilePath,
            LogEventLevel loggerLogLevel,
            bool isDebug,
            bool includeConsole = false)
    {
        var filePath = Path.Join(logFilePath, "Log.");
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(path: $"{filePath}.txt", restrictedToMinimumLevel: loggerLogLevel, rollingInterval: RollingInterval.Hour)
            .WriteTo.File(path: $"{filePath}.Error", restrictedToMinimumLevel: LogEventLevel.Warning, rollingInterval: RollingInterval.Hour)
            .WriteTo.File(path: $"{filePath}.Critical", restrictedToMinimumLevel: LogEventLevel.Fatal, rollingInterval: RollingInterval.Hour);
        if (isDebug)
        {
            configuration = configuration
                .MinimumLevel.Debug()
                .WriteTo.Debug();
        }
        if (includeConsole)
        {
            configuration = configuration
                .WriteTo.Console();
        }

        Log.Logger = configuration.CreateLogger();

        return services
            .AddLogging(builder => builder.AddSerilog(dispose: true));
    }
}