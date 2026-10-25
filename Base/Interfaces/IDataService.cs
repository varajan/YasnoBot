namespace Base.Interfaces;

public interface IDataService
{
    bool IsNotified(object entity);
    void SetNotified(object entity);

    string GetCurrentIcon();
    void SetCurrentIcon(string icon);

    DateTime GetIconUpdateTime();
    void SetIconUpdateTime(DateTime? dateTime = null);
}
