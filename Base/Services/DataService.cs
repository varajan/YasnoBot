using Base.Extensions;
using Base.Interfaces;
using YasnoBot.YasnoLogger.Extensions;
using YasnoLogger.Interfaces;

namespace Base.Services;

public class DataService(IDataBase dataBase) : IDataService
{
    public bool IsNotified(object entity) =>
        dataBase.GetRows($"SELECT * FROM Data WHERE Key = '{entity.GetGuid()}'").Any();

    public void SetNotified(object entity) =>
        dataBase.Execute($"INSERT INTO Data (Key, Value) VALUES ('{entity.GetGuid()}', 'done')");

    public string GetCurrentIcon() => dataBase.GetValue($"SELECT Value FROM Data WHERE Key = 'Icon'");
    public void SetCurrentIcon(string icon) => dataBase.Execute($"INSERT OR REPLACE INTO Data (Key, Value) VALUES ('Icon', '{icon}')");

    public DateTime GetIconUpdateTime()
    {
        try
        {
            return dataBase.GetValue($"SELECT Value FROM Data WHERE Key = 'IconUpdateTime'").ToDateTime();
        }
        catch
        {
            return DateTime.Now.AddHours(-1);
        }
    }

    public void SetIconUpdateTime(DateTime? dateTime = null) =>
        dataBase.Execute($"INSERT OR REPLACE INTO Data (Key, Value) VALUES ('IconUpdateTime', '{dateTime ?? DateTime.Now}')");

}
