namespace YasnoLogger.Interfaces;

public interface ILogger
{
    void Log(Exception exception);
    void Log(string message);
}