using Base;
using Base.DTOs;
using Base.Extensions;
using Base.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using YasnoLogger.Interfaces;

namespace Bot;

internal class Tasks(ITelegramBotClient botClient)
{
    private static ILogger Logger => ServicesProvider.Get<ILogger>();
    private static IUserRepository UserRepository => ServicesProvider.Get<IUserRepository>();
    private static INotifyService NotifyService => ServicesProvider.Get<INotifyService>();
    private static IScheduleService ScheduleService => ServicesProvider.Get<IScheduleService>();
    private static IHistoryService HistoryService => ServicesProvider.Get<IHistoryService>();
    private static IDataBase DataBase => ServicesProvider.Get<IDataBase>();

    private static List<UserDto> UsersToNotify => UserRepository.GetAll().Where(u => u.Notify).ToList();

    public static async Task OnStartUp() => await NotifyUsers("New version is available!");

    public static async Task DoEveryMinute(Task task)
    {
        while (true)
        {
            try
            {
                await task;
                var timeout = DateTime.Now.TillNextMinute();
                await Task.Delay(timeout);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }
    }

    public static async Task NewScheduleNotification()
    {
        var schedule = ScheduleService.GetCurrent();
        if (schedule is not null && !IsNotified(schedule))
        {
            await NotifyUsers("⚠️ New Schedule is available!");
            ScheduleService.SetCurrent(schedule);
            SetNotified(schedule);
        }
    }

    public async Task StatusChangedNotification()
    {
        var lastRecord = HistoryService.GetLastRecord();
        var prevRecord = HistoryService.GetPreviousRecord();

        if (lastRecord is null || prevRecord is null || lastRecord.Notified) return;

        var icon = prevRecord.Status ? "❌" : "💡";
        var message = $"{icon} There was {(prevRecord.Status ? "" : "NO ")}electricity for {prevRecord.DateTime.GetTimeDifference()}";

        await NotifyUsers(message);
        await UpdateIcon(prevRecord!);

        lastRecord.Notified = true;
        HistoryService.Update(lastRecord);
    }

    public async Task UpcomingBlackoutNotification()
    {
        var lastRecord = HistoryService.GetLastRecord();
        if (lastRecord is null || lastRecord.Status) return;

        var schedule = ScheduleService.GetCurrent();
        if (schedule is null || schedule.Today.Slots.Length == 0) return;

        var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);
        var currentSlot = schedule.Today.Slots.Last(s => s.Start < now);

        if (IsNotified(currentSlot) || !currentSlot.IsEnabled || currentSlot.End > now.AddMinutes(5)) return;

        await NotifyUsers("❌ Next blackout is in 5 minutes");
        await UpdateIcon("Images/LightON-OFF.jpeg");
        SetNotified(currentSlot);
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

    private async Task UpdateIcon(StatisticsDto previousData)
    {
        if (previousData.DateTime > DateTime.Now.AddMinutes(-15))
        {
            Logger.Log("Skipping icon update due to time threshold.");
            return;
        }

        var filePath = previousData.Status ? "Images/LightOFF.jpeg" : "Images/LightON.jpeg";
        await UpdateIcon(filePath);
    }

    private async Task UpdateIcon(string filePath)
    {
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var inputPhoto = new InputProfilePhotoStatic { Photo = fileStream };

        await botClient.SetMyProfilePhoto(inputPhoto);
    }

    private static bool IsNotified(object entity) =>
        DataBase.GetRows($"SELECT * FROM Data WHERE Key = '{entity.GetGuid}'").Any();

    private static void SetNotified(object entity) =>
        DataBase.Execute($"INSERT INTO Data (Key, Value) VALUES ('{entity.GetGuid}', '')");
}
