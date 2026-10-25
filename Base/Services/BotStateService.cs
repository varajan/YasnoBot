using Base.Extensions;
using Base.Interfaces;
using YasnoBot.YasnoLogger.Extensions;
using YasnoLogger.Interfaces;

namespace Base.Services;

public class BotStateService(IDataBase dataBase) : IBotStateService
{
    public bool HasBeenNotified(object entity) =>
        dataBase.GetRows($"SELECT * FROM Data WHERE Key = '{entity.GetGuid()}'").Any();

    public void MarkAsNotified(object entity) =>
        dataBase.Execute($"INSERT INTO Data (Key, Value) VALUES ('{entity.GetGuid()}', 'done')");

    public string CurrentIcon
    {
        get => dataBase.GetValue($"SELECT Value FROM Data WHERE Key = 'Icon'");
        set => dataBase.Execute($"INSERT OR REPLACE INTO Data (Key, Value) VALUES ('Icon', '{value}')");
    }

    public DateTime UpdateIconTime
    {
        get
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

        set => dataBase.Execute($"INSERT OR REPLACE INTO Data (Key, Value) VALUES ('IconUpdateTime', '{value}')");
    }
}
