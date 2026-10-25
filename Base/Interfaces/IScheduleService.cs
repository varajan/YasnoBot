using Base.DTOs.Schedule;

namespace Base.Interfaces;

public interface IScheduleService
{
    Task<TwoDayScheduleDto> GetNewSchedule();
    TwoDayScheduleDto GetCurrent();
    void SetCurrent(TwoDayScheduleDto schedule);
}
