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
}