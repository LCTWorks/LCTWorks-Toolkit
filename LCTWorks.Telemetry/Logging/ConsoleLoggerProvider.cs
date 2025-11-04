using Microsoft.Extensions.Logging;

namespace LCTWorks.Telemetry.Logging;

public sealed class ConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger(categoryName);
    }

    public void Dispose()
    { }
}