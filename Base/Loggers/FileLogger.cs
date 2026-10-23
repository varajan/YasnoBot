using YasnoLogger.Interfaces;

namespace YasnoLogger.Loggers;

public class FileLogger : ILogger
{
    private string LogDir => $"{AppDomain.CurrentDomain.BaseDirectory}/Logs";
    private string LogFile => $"{LogDir}/{DateTime.Today:yyyy-MM-dd}.txt";

    public string TopLogs
    {
        get
        {
            try
            {
                if (!File.Exists(LogFile)) return string.Empty;

                var lastLines = File.ReadLines(LogFile).TakeLast(10).ToList();
                return string.Join(Environment.NewLine, lastLines);
            }
            catch { return string.Empty; }
        }
    }

    public FileLogger() => Directory.CreateDirectory(LogDir);

    public void Log(Exception exception)
    {
        Log(exception.Message);
        Log(exception.StackTrace);
    }

    public void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { /*nothing*/ }
    }
}
