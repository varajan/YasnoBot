using Base.Interfaces;

namespace Base.Services;

public class YasnoService : IScheduleService
{
    public (TimeOnly start, TimeOnly end, bool isEnabled)[] GetSchedule()
    {
        var settings = ServicesProvider.Get<ISettings>();

        //var street = settings.Get("Street");
        //var house = settings.Get("House");

        //GetSchedule(street, house);

        // Example schedule: 9 AM to 5 PM, enabled
        return new (TimeOnly, TimeOnly, bool)[]
        {
            (new TimeOnly(9, 0), new TimeOnly(17, 0), true)
        };
    }
    // use random delay

    private string GetSchedule(string street, string house)
    {
        // Implementation for getting schedule
        return string.Empty;
    }

    private string GetSteetCode(string street)
    {

        return street;
    }

    private string GetGroupCode(string streetCode, string house)
    {

        return house;
    }
}
