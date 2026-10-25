using Base;
using Base.DTOs;
using Base.Extensions;
using Base.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using YasnoLogger.Interfaces;

namespace Bot;

internal class Tasks(ITelegramBotClient botClient, IDataService dataService)
{
    private static ILogger Logger => ServicesProvider.Get<ILogger>();
    private static IUserRepository UserRepository => ServicesProvider.Get<IUserRepository>();
    private static INotifyService NotifyService => ServicesProvider.Get<INotifyService>();
    private static IScheduleService ScheduleService => ServicesProvider.Get<IScheduleService>();
    private static IHistoryService HistoryService => ServicesProvider.Get<IHistoryService>();
    private static IDataBase DataBase => ServicesProvider.Get<IDataBase>();

    private static List<UserDto> UsersToNotify => UserRepository.GetAll().Where(u => u.Notify).ToList();

    public static async Task OnStartUp()
    {
        var versionPath = Path.Combine(AppContext.BaseDirectory, "version.txt");
        var version = File.Exists(versionPath) ? File.ReadAllText(versionPath) : string.Empty;

        await NotifyUsers(string.Format(Terms.NewVersionMessage, version));
    }

    private static readonly string iconOn = "Images/LightON.jpeg";
    private static readonly string iconOff = "Images/LightOFF.jpeg";
    private static readonly string iconOnOff = "Images/LightON-OFF.jpeg";

    public static async Task DoEveryMinute(Func<Task> task, int delay)
    {
        while (true)
        {
            try
            {
                await task();
                var timeout = (delay * 1000) + DateTime.Now.TillNextMinute();
                await Task.Delay(timeout);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }
    }

    public async Task NewScheduleNotification()
    {
        var schedule = ScheduleService.GetCurrent();
        if (schedule is null || dataService.IsNotified(schedule)) return;

        Logger.Log("New Schedule is available!");
        await NotifyUsers(schedule.Today.IsEmergence ? Terms.EmergencyBlackoutMessage : Terms.NewScheduleMessage);
        dataService.SetNotified(schedule);
    }

    public async Task UpdateIcon()
    {
        var currentState = HistoryService.GetLastRecord();
        if (currentState?.Status == Status.Blackout)
        {
            await UpdateIcon(iconOff);
            return;
        }

        var schedule = ScheduleService.GetCurrent();
        if (schedule?.Today.IsEmergence == true)
        {
            await UpdateIcon(iconOnOff);
            return;
        }

        var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);
        var in5minutes = now.AddMinutes(5);
        var currentSlot = schedule is null ? default : schedule.Today.Slots.Last(s => s.Start < now);
        if (currentSlot?.Status == Status.Blackout ||
           (currentSlot?.Status == Status.PowerOn && currentSlot.End < in5minutes))
        {
            await UpdateIcon(iconOnOff);
            return;
        }

        await UpdateIcon(iconOn);
    }

    public async Task StatusChangedNotification()
    {
        var currentState = HistoryService.GetLastRecord();
        var previousState = HistoryService.GetLastRecord();
        if (currentState is null || previousState is null || dataService.IsNotified(currentState)) return;

        var messageTemplate = currentState.Status == Status.PowerOn ? Terms.ThereWasNoElectricity : Terms.ThereWasElectricity;
        var message = string.Format(messageTemplate, previousState.DateTime.GetTimeDifference(currentState.DateTime));

        await NotifyUsers(message);
        dataService.SetNotified(currentState);
    }

    public async Task UpcomingBlackoutNotification()
    {
        var currentState = HistoryService.GetLastRecord();
        if (currentState?.Status == Status.Blackout) return;

        var schedule = ScheduleService.GetCurrent();
        if (schedule is null || schedule.Today.Slots.Length == 0) return;

        var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);
        var in5minutes = now.AddMinutes(5);
        var currentSlot = schedule is null ? default : schedule.Today.Slots.Last(s => s.Start < now);
        if (currentSlot?.Status == Status.PowerOn && currentSlot.End < in5minutes && !dataService.IsNotified(currentSlot))
        {
            Logger.Log("Next blackout is in 5 minutes");
            await NotifyUsers(Terms.NextBlackoutMessage);
            dataService.SetNotified(currentSlot);
        }
    }

    private static async Task NotifyUsers(string message)
    {
        foreach (var user in UsersToNotify)
        {
            try
            {
                await NotifyService.Notify(user.Id, message);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to notify user {user.Id}: {e.Message}");
            }
        }
    }

    private async Task UpdateIcon(string filePath)
    {
        if (filePath == dataService.GetCurrentIcon()) return;
        if (DateTime.Now.AddMinutes(-5) < dataService.GetIconUpdateTime()) return;

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var inputPhoto = new InputProfilePhotoStatic { Photo = fileStream };

        await botClient.SetMyProfilePhoto(inputPhoto);
        dataService.SetCurrentIcon(filePath);
        dataService.SetIconUpdateTime();
        Logger.Log($"Update Icon: {filePath}");
    }
}
