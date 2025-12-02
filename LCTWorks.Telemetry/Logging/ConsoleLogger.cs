using Microsoft.Extensions.Logging;

namespace LCTWorks.Telemetry.Logging;

public class ConsoleSimpleLogger(string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var logMessage = formatter(state, exception);
        Console.WriteLine($"[{DateTime.Now}] {categoryName} [{logLevel}]: {logMessage}");
    }
}