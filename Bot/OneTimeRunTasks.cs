using Base.Interfaces;

namespace Bot;

public class OneTimeRunTasks(ILogger logger, INotifyService notifyService, IUserRepository userRepository, IHistoryService historyService, IScheduleService scheduleService)
    : NotificationTasks(logger, notifyService, userRepository)
{
    public async Task DoOnce(Func<Task> task)
    {
        try
        {
            await task();
        }
        catch (Exception e)
        {
            logger.Log(e);
        }
    }

    public async Task SendNewVersionNotification()
    {
        var versionPath = Path.Combine(AppContext.BaseDirectory, "version.txt");
        var version = File.Exists(versionPath) ? File.ReadAllText(versionPath) : string.Empty;

        await NotifyUsers(string.Format(Terms.NewVersionMessage, version));
    }

    public async Task SendCurrentStatusNotification()
    {
        var messages = new Messages(historyService, scheduleService);
        var currentState = messages.GetCurrentStatusMessage();

        if (currentState is not null) await NotifyUsers(currentState);
    }
}
