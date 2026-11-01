using Base.Interfaces;

namespace Bot;

public class OneTimeRunTasks(ILogger logger, INotifyService notifyService, IUserRepository userRepository, IHistoryService historyService, IScheduleService scheduleService)
    : NotificationTasks(logger, notifyService, userRepository)
{
    private Messages Messages => new(historyService, scheduleService);

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

    public async Task SendNewVersionNotification() => await NotifyUsers(Messages.GetVersionMessage());

    public async Task SendCurrentStatusNotification()
    {
        var currentState = Messages.GetCurrentStatusMessage();
        if (currentState is not null) await NotifyUsers(currentState);
    }
}
