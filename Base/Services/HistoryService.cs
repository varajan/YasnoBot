using Base.DTOs.Schedule;
using Base.Extensions;
using Base.Interfaces;
using YasnoBot.YasnoLogger.Extensions;

namespace Base.Services;

public class HistoryService(IDataBase dataBase) : IHistoryService
{
    public StatisticsDto? GetPreviousRecord()
    {
        var lastRecord = dataBase.GetRows("SELECT * FROM History ORDER BY DateTime DESC LIMIT 2").LastOrDefault();
        if (lastRecord is null) return null;

        return GetRecord(lastRecord);
    }

    public StatisticsDto? GetLastRecord()
    {
        var lastRecord = dataBase.GetRows("SELECT * FROM History ORDER BY DateTime DESC LIMIT 1").SingleOrDefault();
        if (lastRecord is null) return null;

        return GetRecord(lastRecord);
    }

    public void AddRecord(Status status)
    {
        var dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        dataBase.Execute($"INSERT INTO History (DateTime, Status) VALUES ('{dateTime}', {(int)status})");
    }

    private static StatisticsDto GetRecord(Dictionary<string, string> record) =>
        new()
        {
            DateTime = record["DateTime"].ToDateTime(),
            Status = (Status)record["Status"].ToInt(),
        };
}
