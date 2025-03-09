using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCTWorks.Common.Services.Telemetry;

public static class TelemetryServiceExtensions
{
    public static IServiceCollection AddSentry(this IServiceCollection services)
    {
        return services.AddSingleton<ITelemetryService, SentryTelemetryServiceInternal>();
    }

    public static IServiceCollection AddSentry(this IServiceCollection services, string sentryDsn, string environment, bool isDebug, TelemetryEnvironmentContextData? contextData = null)
    {
        var sentryService = new SentryTelemetryServiceInternal();
        sentryService.Initialize(sentryDsn, environment, isDebug, contextData);
        return services.AddSingleton<ITelemetryService>(sentryService);
    }
}