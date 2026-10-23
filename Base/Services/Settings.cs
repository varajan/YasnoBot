using Base.Interfaces;
using YasnoLogger.Interfaces;

namespace Base.Services;

public class Settings(IDataBase dataBase) : ISettings
{
    public List<string> Keys => dataBase.GetColumn("SELECT Key FROM Settings").ToList();

    public string Get(string key) => dataBase.GetValue($"SELECT Value FROM Settings WHERE Key = '{key}' COLLATE NOCASE");

    public void Set(string key, string value) => dataBase.Execute($"INSERT OR REPLACE INTO Settings (Key, Value) VALUES ('{key}', '{value}')");
}
