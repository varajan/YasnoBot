namespace Base.Interfaces;

public interface ISettings
{
    List<string> Keys { get; }
    string Get(string key);
    void Set(string key, string value);
}
