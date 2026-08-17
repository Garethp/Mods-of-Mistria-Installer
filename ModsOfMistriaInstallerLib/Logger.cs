using System.Diagnostics.CodeAnalysis;

namespace Garethp.ModsOfMistriaInstallerLib;

public class LogAddedEventArgs : EventArgs
{
    public string Message { get; }
    
    public LogAddedEventArgs(string message)
    {
        Message = message;
    }
}

public class Logger
{
    public static event EventHandler<LogAddedEventArgs> LogAdded; 
    
    private static readonly List<string> Logs = [];
    private static readonly object Sync = new();
    
    public static void Log(string message)
    {
        Add(message);
    }
    
    public static void Log([StringSyntax("CompositeFormat")] string format, params object[] args)
    {
        Add(string.Format(format, args));
    }
    
    public static List<string> GetLogs()
    {
        lock (Sync)
            return [.. Logs];
    }

    private static void Add(string message)
    {
        lock (Sync)
            Logs.Add(message);

        // Notify outside the lock: the UI subscriber may synchronously read a
        // snapshot of the log and must never be able to deadlock the writer.
        LogAdded?.Invoke(null, new LogAddedEventArgs(message));
    }
}
