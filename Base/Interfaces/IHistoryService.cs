using Base.DTOs;

namespace Base.Interfaces;

public interface IHistoryService
{
    StatisticsDto? GetLastRecord();
    StatisticsDto? GetPreviousRecord();
    void AddRecord(Status status);
}
