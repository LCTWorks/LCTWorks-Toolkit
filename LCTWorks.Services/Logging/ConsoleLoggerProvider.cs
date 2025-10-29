using Microsoft.Extensions.Logging;

namespace LCTWorks.Services.Logging;

public sealed class ConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger(categoryName);
    }

    public void Dispose()
    { }
}