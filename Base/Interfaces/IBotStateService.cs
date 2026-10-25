namespace Base.Interfaces;

public interface IBotStateService
{
    bool HasBeenNotified(object entity);
    void MarkAsNotified(object entity);
    string CurrentIcon { get; set; }
    DateTime UpdateIconTime { get; set; }
}
