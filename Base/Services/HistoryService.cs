using Base.DTOs;
using Base.Interfaces;
using YasnoBot.YasnoLogger.Extensions;
using YasnoLogger.Interfaces;

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

    public void AddRecord(bool status)
    {
        var dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var active = status ? 1 : 0;
        dataBase.Execute($"INSERT INTO History (DateTime, Active, Notified) VALUES ('{dateTime}', {active}, 0)");
    }

    public void Update(StatisticsDto record)
    {
        var dateTime = record.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
        var active = record.Status ? 1 : 0;
        var notified = record.Notified ? 1 : 0;
        dataBase.Execute($"UPDATE History SET Active = {active}, Notified = {notified} WHERE DateTime = '{dateTime}'");
    }

    private static StatisticsDto GetRecord(Dictionary<string, string> record) =>
        new()
        {
            DateTime = record["DateTime"].ToDateTime(),
            Status = record["Active"].ToInt() > 0,
            Notified = record["Notified"].ToInt() > 0,
        };
}
