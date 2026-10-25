using Base.DTOs.Schedule;

namespace Base.Interfaces;

public interface IHistoryService
{
    StatisticsDto? GetLastRecord();
    StatisticsDto? GetPreviousRecord();
    void AddRecord(Status status);
}
