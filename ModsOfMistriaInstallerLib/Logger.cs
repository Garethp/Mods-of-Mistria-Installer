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
    
    public static void Log(string message)
    {
        Logs.Add(message);
        LogAdded?.Invoke(null, new LogAddedEventArgs(message));
    }
    
    public static void Log([StringSyntax("CompositeFormat")] string format, params object[] args)
    {
        Logs.Add(string.Format(format, args));
        LogAdded?.Invoke(null, new LogAddedEventArgs(string.Format(format, args)));
    }
    
    public static List<string> GetLogs()
    {
        return Logs;
    }
}