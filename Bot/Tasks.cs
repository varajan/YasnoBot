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

    private static List<UserDto> UsersToNotify => UserRepository.GetAll().Where(u => u.Notify).ToList();

    public async Task OnStartUp() => await NotifyUsers("New version is available!");

    public async Task DoEveryMinute()
    {
        while (true)
        {
            try
            {
                var schedule = ScheduleService.Current;
                if (schedule is not null && !schedule.Notified)
                {
                    await NotifyUsers("⚠️ New Schedule is available!");
                    schedule.Notified = true;
                    ScheduleService.Current = schedule;
                }

                var historyService = ServicesProvider.Get<IHistoryService>();
                var lastRecord = historyService.GetLastRecord();
                var prevRecord = historyService.GetPreviousRecord();

                if (lastRecord is null || prevRecord is null || lastRecord.Notified) continue;

                var icon = prevRecord.Status ? "❌" : "💡";
                var message = $"{icon} There was {(prevRecord.Status ? "" : "NO ")}electricity for {prevRecord.DateTime.GetTimeDifference()}";

                await NotifyUsers(message);
                await UpdateIcon(prevRecord!);

                lastRecord.Notified = true;
                historyService.Update(lastRecord);

                var timeout = DateTime.Now.TillNextMinute();
                await Task.Delay(timeout);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
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

    private async Task UpdateIcon(StatisticsDto previousData)
    {
        if (previousData.DateTime > DateTime.Now.AddMinutes(-15))
        {
            Logger.Log("Skipping icon update due to time threshold.");
            return;
        }

        var filePath = previousData.Status ? "Images/LightOFF.jpeg" : "Images/LightON.jpeg";

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var inputPhoto = new InputProfilePhotoStatic { Photo = fileStream };

        await botClient.SetMyProfilePhoto(inputPhoto);
    }
}
