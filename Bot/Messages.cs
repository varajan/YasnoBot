using Base.DTOs.Schedule;
using Base.Extensions;
using Base.Interfaces;

namespace Bot;

public class Messages(IHistoryService historyService, IScheduleService scheduleService)
{
    public string? GetCurrentStatusMessage()
    {
        var currentState = historyService.GetLastRecord();
        if (currentState is null) return null;

        var messageTemplate = currentState.Status == Status.PowerOn ? Terms.ThereIsElectricity : Terms.ThereIsNoElectricity;
        var duration = currentState.DateTime.GetTimeDifference();
        var nextChange = this.GetNextStatusChange(currentState);
        return string.Format(messageTemplate, duration, nextChange);
    }

    public string? GetScheduleMessage(bool isToday)
    {
        var fullSchedule = scheduleService.GetCurrent();
        if (fullSchedule is null) return null;

        var schedule = isToday ? fullSchedule.Today : fullSchedule.Tomorrow;
        return ScheduledTasks.GetScheduleMessage(schedule, isToday);
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
}
