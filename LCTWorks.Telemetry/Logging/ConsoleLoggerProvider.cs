using Microsoft.Extensions.Logging;

namespace LCTWorks.Telemetry.Logging;

public sealed class ConsoleSimpleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleSimpleLogger(categoryName);
    }

    public void Dispose()
    { }
}