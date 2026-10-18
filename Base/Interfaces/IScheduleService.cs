namespace Base.Interfaces;

public interface IScheduleService
{
    (TimeOnly start, TimeOnly end, bool isEnabled)[] GetSchedule();
}
