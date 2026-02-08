namespace LCTWorks.Core.Interops;

public static class ConsoleInterops
{
    private static readonly Lock _lock = new();
    private static ConsoleColor _foregroundColor = ConsoleColor.Gray;

    public static event EventHandler? ClearConsoleRequested;

    public static ConsoleColor ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            lock (_lock)
            {
                _foregroundColor = value;
                Console.ForegroundColor = value;
            }
        }
    }

    public static void ClearConsole()
    {
        lock (_lock)
        {
            ClearConsoleRequested?.Invoke(null, EventArgs.Empty);
            ResetForegroundColor();
        }
    }

    public static void ResetForegroundColor()
           => ForegroundColor = ConsoleColor.Gray;

    public static void WriteErrorLine(string message)
    {
        WriteLine(message, ConsoleColor.Red);
    }

    public static void WriteLine(string message, ConsoleColor color = ConsoleColor.White, bool includeTimeSpan = true)
    {
        var time = DateTime.Now.ToLocalTime();
        if (includeTimeSpan)
        {
            message = $"[{time:HH:mm:ss}] {message}";
        }
        ForegroundColor = color;
        Console.WriteLine(message);
        ResetForegroundColor();
    }

    public static void WriteSuccessLine(string message)
    {
        WriteLine(message, ConsoleColor.Green);
    }

    public static void WriteWarningLine(string message)
    {
        WriteLine(message, ConsoleColor.Yellow);
    }
}