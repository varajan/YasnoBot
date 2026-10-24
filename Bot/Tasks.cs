using Base;
using Base.DTOs;
using Base.Extensions;
using Base.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using YasnoBot.YasnoLogger.Extensions;
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

    public static async Task OnStartUp()
    {
        var versionPath = Path.Combine(AppContext.BaseDirectory, "version.txt");
        var version = File.ReadAllText(versionPath);

        await NotifyUsers($"New version is available: {version}");
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

    public static async Task NewScheduleNotification()
    {
        var schedule = ScheduleService.GetCurrent();
        if (schedule is null || IsNotified(schedule)) return;

        Logger.Log("New Schedule is available!");
        await NotifyUsers(schedule.Today.IsEmergence ? "‼ Emergency blackouts!" : "🗓 New Schedule is available!");
        SetNotified(schedule);
    }

    public async Task UpdateIcon()
    {
        var schedule = ScheduleService.GetCurrent();
        var currentStatus = HistoryService.GetLastRecord();

        if (currentStatus is not null && currentStatus.Status && schedule.Today.IsEmergence)
        {
            await UpdateIcon(iconOnOff);
            return;
        }

        if (currentStatus is null || !currentStatus.Status || currentStatus.DateTime.AddMinutes(15) < DateTime.Now) return;
        await UpdateIcon(iconOn);
    }

    public async Task StatusChangedNotification()
    {
        var lastRecord = HistoryService.GetLastRecord();
        var prevRecord = HistoryService.GetPreviousRecord();

        if (lastRecord is null || prevRecord is null || IsNotified(lastRecord)) return;

        var icon = prevRecord.Status ? "🪫" : "🔋";
        var message = $"{icon} There was {(prevRecord.Status ? "" : "NO ")}electricity for {prevRecord.DateTime.GetTimeDifference(lastRecord.DateTime)}";

        await NotifyUsers(message);
        await UpdateIcon(lastRecord.Status ? iconOn : iconOff);

        SetNotified(lastRecord);
    }

    public async Task UpcomingBlackoutNotification()
    {
        var lastRecord = HistoryService.GetLastRecord();
        if (lastRecord is null || !lastRecord.Status) return;

        var schedule = ScheduleService.GetCurrent();
        if (schedule is null || schedule.Today.Slots.Length == 0) return;

        var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);
        var currentSlot = schedule.Today.Slots.Last(s => s.Start < now);

        if (IsNotified(currentSlot) || !currentSlot.IsEnabled || currentSlot.End > now.AddMinutes(5)) return;

        Logger.Log("Next blackout is in 5 minutes");
        await NotifyUsers("🪫 Next blackout is in 5 minutes");
        await UpdateIcon(iconOnOff);
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

    private async Task UpdateIcon(string filePath)
    {
        if (filePath == GetCurrentIcon()) return;
        if (DateTime.Now.AddMinutes(-5) > GetIconUpdateTime()) return;

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var inputPhoto = new InputProfilePhotoStatic { Photo = fileStream };

        await botClient.SetMyProfilePhoto(inputPhoto);
        SetCurrentIcon(filePath);
        SetIconUpdateTime();
        Logger.Log($"Update Icon: {filePath}");
    }

    private static bool IsNotified(object entity) =>
        DataBase.GetRows($"SELECT * FROM Data WHERE Key = '{entity.GetGuid()}'").Any();

    private static void SetNotified(object entity) =>
        DataBase.Execute($"INSERT INTO Data (Key, Value) VALUES ('{entity.GetGuid()}', 'done')");

    private static string GetCurrentIcon() => DataBase.GetValue($"SELECT Value FROM Data WHERE Key = 'Icon'");

    private static void SetCurrentIcon(string icon) => DataBase.Execute($"INSERT OR UPDATE INTO Data (Key, Value) VALUES ('Icon', '{icon}')");

    private static DateTime GetIconUpdateTime()
    {
        try
        {
            return DataBase.GetValue($"SELECT Value FROM Data WHERE Key = 'IconUpdateTime'").ToDateTime();
        }
        catch
        {
            return DateTime.Now.AddHours(-1);
        }
    }

    private static void SetIconUpdateTime() => DataBase.Execute($"INSERT OR UPDATE INTO Data (Key, Value) VALUES ('IconUpdateTime', '{DateTime.Now}')");
}
