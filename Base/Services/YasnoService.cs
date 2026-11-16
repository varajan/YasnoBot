using Base.DTOs.Schedule;
using Base.DTOs.Yasno;
using Base.Extensions;
using Base.Interfaces;

namespace Base.Services;

public class YasnoService(IApi api, IDataBase database) : IScheduleService
{
    private readonly string addressApiUrl = "https://app.yasno.ua/api/blackout-service/public/shutdowns/addresses/v2";

    public TwoDayScheduleDto GetCurrent() => database.GetValue("SELECT Value FROM Data WHERE Key = 'Schedule'")?.DeSerialize<TwoDayScheduleDto>();

    public void SetCurrent(TwoDayScheduleDto schedule) => database.Execute($"INSERT OR REPLACE INTO Data (Key, Value) VALUES ('Schedule', '{schedule.Serialize()}')");

    public async Task<List<string>> GetCities(int waitTimeout = 0)
    {
        await RandomDelay(waitTimeout);

        var regions = await api.Get<RegionDto[]>($"{addressApiUrl}/regions");
        var cities = regions?.Select(r => r.Value).ToList();

        return cities ?? [];
    }

    public async Task<List<string>> GetDsoses(int waitTimeout = 0)
    {
        await RandomDelay(waitTimeout);

        var settings = ServicesProvider.Get<ISettings>();
        var city = settings.Get(Constants.CITY);
        var regions = await api.Get<RegionDto[]>($"{addressApiUrl}/regions");
        var dsoses = regions?.FirstOrDefault(r => r.Value == city)?.Dsos.Select(d => d.Name).ToList();

        return dsoses ?? [];
    }

    public async Task<List<string>> GetHouses(int waitTimeout = 0)
    {
        await RandomDelay(waitTimeout);

        var settings = ServicesProvider.Get<ISettings>();
        var city = settings.Get(Constants.CITY);
        var dsos = settings.Get(Constants.DSOS);
        var street = settings.Get(Constants.STREET);

        var (cityId, dsosId) = await GetRegionData(city, dsos);
        var streetId = await GetStreetId(street, cityId, dsosId);
        var houses = await api.Get<BaseDto[]>($"{addressApiUrl}/houses?regionId={cityId}&streetId={streetId}&query=&dsoId={dsosId}");

        return houses?.Select(h => h.Value).ToList() ?? [];
    }

    public async Task<TwoDayScheduleDto> GetNewSchedule()
    {
        var settings = ServicesProvider.Get<ISettings>();

        var city = settings.Get(Constants.CITY);
        var dsos = settings.Get(Constants.DSOS);
        var street = settings.Get(Constants.STREET);
        var house = settings.Get(Constants.HOUSE);

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(dsos) || string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(house))
        {
            throw new Exception("City, DSOS, Street, and House settings must be set.");
        }

        var (cityId, dsosId) = await GetRegionData(city, dsos);
        var streetId = await GetStreetId(street, cityId, dsosId);
        var houseId = await GetHouseId(house, cityId, dsosId, streetId);
        var group = await GetGroup(cityId, dsosId, streetId, houseId);
        var schedule = await GetSchedule(group, cityId, dsosId);

        var today = schedule.Today.Slots.Select(GetSlot).ToArray();
        var tomorrow = schedule.Tomorrow.Slots.Select(GetSlot).ToArray();

        return new TwoDayScheduleDto
        {
            Today = new ScheduleDto
            {
                Slots = today,
                Date = schedule.Today.Date,
                IsEmergence = schedule.Today.Status == "EmergencyShutdowns"
            },

            Tomorrow = new ScheduleDto
            {
                Slots = tomorrow,
                Date = schedule.Tomorrow.Date,
                IsEmergence = schedule.Tomorrow.Status == "EmergencyShutdowns"
            },
        };
    }

    private DTOs.Schedule.SlotDto GetSlot(DTOs.Yasno.SlotDto slot)
    {
        TimeOnly start = new(slot.Start / 60, slot.Start % 60);
        TimeOnly end = slot.End == 1440
            ? new TimeOnly(23, 59, 59, 999)
            : new TimeOnly(slot.End / 60, slot.End % 60);
        var status = slot.Type == "NotPlanned" ? Status.PowerOn : Status.Blackout;

        return new DTOs.Schedule.SlotDto { Start = start, End = end, Status = status };
    }

    public async Task<(int cityId, int dsosId)> GetRegionData(string city, string dsos, int waitTimeout = 0)
    {
        await RandomDelay(waitTimeout);

        var regions = await api.Get<RegionDto[]>($"{addressApiUrl}/regions");
        var region = regions!.First(r => r.Value == city);
        var cityId = region.Id;
        var dsosId = region.Dsos.First(d => d.Name.Contains(dsos, StringComparison.CurrentCultureIgnoreCase)).Id;

        return (cityId, dsosId);
    }

    public async Task<int> GetStreetId(string street, int cityId, int dsosId, int waitTimeout = 0)
    {
        await RandomDelay(waitTimeout);

        var streets = await api.Get<BaseDto[]>($"{addressApiUrl}/streets?regionId={cityId}&query={street}&dsoId={dsosId}");
        var streetId = streets!.FirstOrDefault(d => d.Value.Contains(street, StringComparison.CurrentCultureIgnoreCase))?.Id;

        return streetId ?? -1;
    }

    public async Task<int> GetHouseId(string house, int cityId, int dsosId, int streetId, int waitTimeout = 0)
    {
        await RandomDelay(waitTimeout);

        var houses = await api.Get<BaseDto[]>($"{addressApiUrl}/houses?regionId={cityId}&streetId={streetId}&query=&dsoId={dsosId}");
        var houseId = houses!.FirstOrDefault(d => d.Value.Contains(house, StringComparison.CurrentCultureIgnoreCase))?.Id;

        return houseId ?? -1;
    }

    private async Task<GroupDto> GetGroup(int cityId, int dsosId, int streetId, int houseId)
    {
        await RandomDelay();

        var group = await api.Get<GroupDto>($"{addressApiUrl}/group?regionId={cityId}&streetId={streetId}&houseId={houseId}&dsoId={dsosId}");

        return group!;
    }

    private async Task<GroupScheduleDto> GetSchedule(GroupDto group, int cityId, int dsosId)
    {
        await RandomDelay();

        var plannedOutagesApiUrl = $"https://app.yasno.ua/api/blackout-service/public/shutdowns/regions/{cityId}/dsos/{dsosId}/planned-outages";
        var fullSchedule = await api.Get<ScheduleResponse>(plannedOutagesApiUrl);
        var schedule = fullSchedule![$"{group!.Group}.{group.Subgroup}"];

        return schedule;
    }

    private static async Task RandomDelay(int waitTimeout = 0)
    {
        int delayMilliseconds = waitTimeout == 0 ? Random.Shared.Next(500, 2500) : waitTimeout;
        await Task.Delay(delayMilliseconds);
    }
}
