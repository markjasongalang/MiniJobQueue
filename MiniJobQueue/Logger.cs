namespace MiniJobQueue;

public static class Logger
{
    private static readonly object LockObj = new();

    public static void LogInfo(string category, string message)
        => WriteLog(category, message, ConsoleColor.Cyan);

    public static void LogSuccess(string category, string message)
        => WriteLog(category, message, ConsoleColor.Green);

    public static void LogWarning(string category, string message)
        => WriteLog(category, message, ConsoleColor.Yellow);

    public static void LogError(string category, string message)
        => WriteLog(category, message, ConsoleColor.Red);

    private static void WriteLog(string category, string message, ConsoleColor color)
    {
        lock (LockObj)
        {
            Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = color;
            Console.Write($"[{category}] ");
            Console.ResetColor();
            Console.WriteLine(message);
        }
    }
}
