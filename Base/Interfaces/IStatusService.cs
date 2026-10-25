using Base.DTOs.Schedule;

namespace Base.Interfaces;

public interface IStatusService
{
    Status GetCurrentStatus();
}
