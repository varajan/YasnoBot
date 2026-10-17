namespace Base.Interfaces;

public interface INotifyService
{
    Task NotifyWithButtons(long userId, string message, params string[] buttons);
    Task Notify(long userId, string message);
}