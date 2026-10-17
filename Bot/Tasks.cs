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

    public async Task DoEveryMinute()
    {
        while (true)
        {
            try
            {
                var historyService = ServicesProvider.Get<IHistoryService>();
                var statusService = ServicesProvider.Get<IStatusService>();

                var lastStatus = historyService.GetLastRecord()?.Status ?? false;
                var isRunning = statusService.IsRunning();

                if (lastStatus != isRunning)
                {
                    await UpdateIcon(isRunning);

                    var prevRecord = historyService.GetPreviousRecord();
                    await NotifyUsers(prevRecord!);
                }

                var timeout = DateTime.Now.TillNextMinute();
                await Task.Delay(timeout);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
        }
    }

    private static async Task NotifyUsers(StatisticsDto previousData)
    {
        var users = UserRepository.GetAll().Where(u => u.Notify).ToList();
        var icon = previousData.Status ? "❌" : "💡";
        var message = $"{icon} There was {(previousData.Status ? "" : "NO ")}electricity for {previousData.DateTime.GetTimeDifference()}";

        foreach (var user in users)
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

    private async Task UpdateIcon(bool isRunning)
    {
        var filePath = isRunning ? "Images/LightON.jpeg" : "Images/LightOFF.jpeg";

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var inputPhoto = new InputProfilePhotoStatic { Photo = fileStream };

        await botClient.SetMyProfilePhoto(inputPhoto);
    }
}
