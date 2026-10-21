namespace OrderProcessing.Shared.Logging;
public static class Log
{
    static readonly object Sync = new();
    public static void Info(string message) => Write(message, ConsoleColor.Gray);
    public static void Success(string message) => Write(message, ConsoleColor.Green);
    public static void Error(string message) => Write(message, ConsoleColor.Red);
    static void Write(string message, ConsoleColor color)
    {
        lock (Sync)
        {
            var old = Console.ForegroundColor; Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Thread {Environment.CurrentManagedThreadId}] {message}");
            Console.ForegroundColor = old;
        }
    }
}
