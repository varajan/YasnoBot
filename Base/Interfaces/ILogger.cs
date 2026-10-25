namespace Base.Interfaces;

public interface ILogger
{
    string TopLogs { get; }
    void Log(Exception exception);
    void Log(string message);
}