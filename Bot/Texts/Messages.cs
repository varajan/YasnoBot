using Base;
using Base.DTOs.Schedule;
using Base.Extensions;
using Base.Interfaces;

namespace Bot.Texts;

public class Messages(IHistoryService historyService, IScheduleService scheduleService)
{
    public string GetVersionMessage()
    {
        var versionPath = Path.Combine(AppContext.BaseDirectory, "version.txt");
        var version = File.Exists(versionPath) ? File.ReadAllText(versionPath) : string.Empty;

        return string.Format(Terms.NewVersionMessage, version);
    }

    public string? GetCurrentStatusMessage()
    {
        var currentState = historyService.GetLastRecord();
        if (currentState is null) return null;

        var messageTemplate = currentState.Status == Status.PowerOn ? Terms.ThereIsElectricity : Terms.ThereIsNoElectricity;
        var duration = currentState.DateTime.GetTimeDifference();
        var nextChange = this.GetNextStatusChange(currentState);
        return string.Format(messageTemplate, duration, nextChange);
    }

    public string? GetScheduleMessage(bool today)
    {
        var fullSchedule = scheduleService.GetCurrent();
        if (fullSchedule is null) return null;

        var schedule = today ? fullSchedule.Today : fullSchedule.Tomorrow;

        if (schedule?.IsEmergence == true) return Terms.EmergencyBlackout;

        if (schedule is null || schedule.Slots is null || schedule.Slots.Length == 0)
        {
            return Terms.NoScheduleAvailable;
        }

        var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);
        var message = string.Empty;
        var onTime = TimeSpan.Zero;
        var offTime = TimeSpan.Zero;

        foreach (var slot in schedule.Slots)
        {
            var arrow = today && now.IsBetween(slot.Start, slot.End) ? Icons.Arrow : string.Empty;
            var status = slot.Status == Status.PowerOn ? Icons.BatteryFull : Icons.BatteryEmpty;
            message += $"{slot.Start:HH:mm} - {slot.End:HH:mm} {status} {arrow}\n";

            var duration = slot.End - slot.Start;
            if (slot.Status == Status.PowerOn) onTime += duration; else offTime += duration;
        }

        onTime = TimeSpan.FromMinutes(Math.Round(onTime.TotalMinutes / 30.0) * 30);
        offTime = TimeSpan.FromMinutes(Math.Round(offTime.TotalMinutes / 30.0) * 30);

        message += $"\n{Icons.BatteryFull} {onTime:hh\\:mm}";
        message += $"\n{Icons.BatteryEmpty} {offTime:hh\\:mm}";

        return message;
    }

    public string GetNextStatusChange(StatisticsDto currentState)
    {
        var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);
        var schedule = scheduleService.GetCurrent();
        var currentSlot = schedule?.Today.Slots.LastOrDefault(s => s.Start < now) ??
                          schedule?.Tomorrow?.Slots.FirstOrDefault();

        if (schedule?.Today.IsEmergence == true) return Terms.Unknown + Environment.NewLine + Terms.EmergencyBlackout;

        if (currentSlot is null) return Terms.Unknown;

        return currentState.Status == currentSlot.Status
            ? currentSlot.End.ToString("HH:mm")
            : Terms.ExpectedAt.ToLower() + currentSlot.Start.ToString("HH:mm");
    }

    public string SettingsMessage(ISettings settings)
    {
        if (settings is null || settings.Keys.Count == 0) return Terms.NoSettings;
        var allSettings = new List<string>
        {
            GetSettingMessage(settings, Constants.CITY, Terms.City),
            GetSettingMessage(settings, Constants.DSOS, Terms.Dsos),
            GetSettingMessage(settings, Constants.STREET, Terms.Street),
            GetSettingMessage(settings, Constants.HOUSE, Terms.House),
            GetSettingMessage(settings, Constants.IPS, Terms.IPs)
        };

        return string.Join("\n", allSettings);
    }

    public string GetSettingMessage(ISettings settings, string key, string name)
    {
        var value = settings.Get(key);
        return $"{name}: {(string.IsNullOrEmpty(value) ? Terms.NoDataAvailable : value)}";
    }

    //public string GetAvaliableOptionsMessage(string key)
    //{
    //    scheduleService
    //}
}
