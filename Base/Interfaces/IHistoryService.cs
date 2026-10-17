using Base.DTOs;

namespace Base.Interfaces;

public interface IHistoryService
{
    StatisticsDto? GetLastRecord();
    StatisticsDto? GetPreviousRecord();
    void AddRecord(bool status);
}
