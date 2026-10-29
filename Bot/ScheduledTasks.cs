using Base.DTOs;
using Base.DTOs.Schedule;
using Base.Extensions;
using Base.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bot;

public class ScheduledTasks(
    ITelegramBotClient botClient,
    ILogger logger,
    INotifyService notifyService,
    IScheduleService scheduleService,
    IUserRepository userRepository,
    IStatusService statusService,
    IHistoryService historyService,
    IBotStateService botStateService)
{
    private List<UserDto> UsersToNotify => userRepository.GetAll().Where(u => u.Notify).ToList();

    public async Task SendNewVersionNotification()
    {
        var versionPath = Path.Combine(AppContext.BaseDirectory, "version.txt");
        var version = File.Exists(versionPath) ? File.ReadAllText(versionPath) : string.Empty;

        await NotifyUsers(string.Format(Terms.NewVersionMessage, version));
    }

    private static readonly string iconOn = "Images/LightON.jpeg";
    private static readonly string iconOff = "Images/LightOFF.jpeg";
    private static readonly string iconOnOff = "Images/LightON-OFF.jpeg";

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

    public async Task DoEveryMinute(Func<Task> task, int delay)
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
                logger.Log(e);
            }
        }
    }

    public async Task DoEvery10Minutes(Func<Task> task)
    {
        while (true)
        {
            try
            {
                await task();
                var timeout = (9 * 60_000) + DateTime.Now.TillNextMinute();
                await Task.Delay(timeout);
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }
    }

    public async Task NewScheduleNotification()
    {
        var schedule = scheduleService.GetCurrent();
        if (schedule is null) return;

        await NotifyAboutNewSchedule(schedule.Today, true);
        await NotifyAboutNewSchedule(schedule.Tomorrow);
    }
    
    private async Task NotifyAboutNewSchedule(ScheduleDto schedule, bool today = false)
    {
        if (botStateService.HasBeenNotified(schedule)) return;
        logger.Log("New Schedule is available!");

        if (today && schedule.IsEmergence)
        {
            await NotifyUsers(Terms.EmergencyBlackoutMessage);
            botStateService.MarkAsNotified(schedule);
            return;
        }

        var message = GetScheduleMessage(schedule, today);
        await NotifyUsers(today ? Terms.NewScheduleMessage4Today : Terms.NewScheduleMessage4Tomorrow);
        await NotifyUsers(message);
        botStateService.MarkAsNotified(schedule);
    }

    public async Task UpdateIcon()
    {
        var currentState = historyService.GetLastRecord();
        if (currentState?.Status == Status.Blackout)
        {
            await UpdateIcon(iconOff);
            return;
        }

        var schedule = scheduleService.GetCurrent();
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
        var currentState = historyService.GetLastRecord();
        var previousState = historyService.GetPreviousRecord();
        if (currentState is null || previousState is null || botStateService.HasBeenNotified(currentState)) return;

        var messageTemplate = currentState.Status == Status.PowerOn ? Terms.ThereWasNoElectricity : Terms.ThereWasElectricity;
        var message = string.Format(messageTemplate, previousState.DateTime.GetTimeDifference(currentState.DateTime));

        await NotifyUsers(message);
        botStateService.MarkAsNotified(currentState);
    }

    public async Task UpcomingBlackoutNotification()
    {
        var currentState = historyService.GetLastRecord();
        if (currentState?.Status == Status.Blackout) return;

        var schedule = scheduleService.GetCurrent();
        if (schedule is null || schedule.Today.Slots.Length == 0) return;

        var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);
        var in5minutes = now.AddMinutes(5);
        var currentSlot = schedule is null ? default : schedule.Today.Slots.Last(s => s.Start < now);
        if (currentSlot?.Status == Status.PowerOn && currentSlot.End < in5minutes && !botStateService.HasBeenNotified(currentSlot))
        {
            logger.Log("Next blackout is in 5 minutes");
            await NotifyUsers(Terms.NextBlackoutMessage);
            botStateService.MarkAsNotified(currentSlot);
        }
    }

    public async Task GetCurrentStatus()
    {
        var currentStatus = statusService.GetCurrentStatus();
        var lastStatus = historyService.GetLastRecord()?.Status ?? Status.Blackout;

        if (lastStatus != currentStatus)
        {
            historyService.AddRecord(currentStatus);
            logger.Log($"Status changed: {lastStatus} -> {currentStatus}");
        }
    }

    public async Task UpdateSchedule()
    {
        var newSchedule = await scheduleService.GetNewSchedule();
        var currentSchedule = scheduleService.GetCurrent();

        if (currentSchedule is null || !currentSchedule.IsEqual(newSchedule))
        {
            logger.Log("Updating schedule.");
            scheduleService.SetCurrent(newSchedule);

            if (currentSchedule?.Tomorrow?.Date == newSchedule.Today?.Date)
            {
                botStateService.MarkAsNotified(newSchedule.Today);
                botStateService.MarkAsNotified(newSchedule.Tomorrow);
            }
        }
    }

    public string GetScheduleMessage(ScheduleDto schedule, bool today)
    {
        if (schedule is null || schedule.Slots is null || !schedule.Slots.Any())
        {
            return Terms.NoScheduleAvailable;
        }

        var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);
        var message = string.Empty;
        if (schedule.IsEmergence) message += Terms.EmergencyBlackout;

        foreach (var slot in schedule.Slots)
        {
            var arrow = today && now.IsBetween(slot.Start, slot.End) ? Icons.Arrow : string.Empty;
            var status = slot.Status == Status.PowerOn ? Icons.BatteryFull : Icons.BatteryEmpty;
            message += $"{slot.Start:HH:mm} - {slot.End:HH:mm} {status} {arrow}\n";
        }
        return message;
    }
    
    private async Task NotifyUsers(string message)
    {
        foreach (var user in UsersToNotify)
        {
            try
            {
                await notifyService.Notify(user.Id, message);
            }
            catch (Exception e)
            {
                logger.Log($"Failed to notify user {user.Id}: {e.Message}");
            }
        }
    }

    private async Task UpdateIcon(string filePath)
    {
        if (filePath == botStateService.CurrentIcon) return;
        if (DateTime.Now.AddMinutes(-5) < botStateService.UpdateIconTime) return;

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var inputPhoto = new InputProfilePhotoStatic { Photo = fileStream };

        await botClient.SetMyProfilePhoto(inputPhoto);
        botStateService.CurrentIcon = filePath;
        botStateService.UpdateIconTime = DateTime.Now;
        logger.Log($"Update Icon: {filePath}");
    }
}
