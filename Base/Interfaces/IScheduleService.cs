using Base.DTOs;

namespace Base.Interfaces;

public interface IScheduleService
{
    Task<TwoDayScheduleDto> GetNewSchedule();
    TwoDayScheduleDto Current { get; set; }
}
