using Base.DTOs;
using Base.Extensions;
using Base.Interfaces;
using YasnoLogger.Interfaces;

namespace Base.Services;

public class YasnoService(IApi api, IDataBase database, ILogger logger) : IScheduleService
{
    private readonly string addressApiUrl = "https://app.yasno.ua/api/blackout-service/public/shutdowns/addresses/v2";

    public TwoDayScheduleDto Current
    {
        get => database.GetValue("SELECT Value WHERE Key = 'Schedule'").DeSerialize<TwoDayScheduleDto>();
        set => database.Execute($"INSERT OR REPLACE INTO Data (Key, Value) VALUES ('Schedule', '{value.Serialize()}')");
    }

    public async Task<TwoDayScheduleDto> GetNewSchedule()
    {
        var settings = ServicesProvider.Get<ISettings>();
        var city = settings.Get("City");
        var dsos = settings.Get("DSOS");
        var street = settings.Get("Street");
        var house = settings.Get("House");

        var (cityId, dsosId) = await GetRegionData(city, dsos);
        var streetId = await GetStreetId(street, cityId, dsosId);
        var houseId = await GetHouseId(house, cityId, dsosId, streetId);
        var group = await GetGroup(cityId, dsosId, streetId, houseId);
        var schedule = await GetSchedule(group, cityId, dsosId);

        var today = schedule.Today.Slots.Select(GetSlot).ToArray();
        var tomorrow = schedule.Tomorrow.Slots.Select(GetSlot).ToArray();

        return new TwoDayScheduleDto
        {
            Today = new ScheduleDto { Slots = today },
            Tomorrow = new ScheduleDto { Slots = tomorrow },
        };
    }

    private ScheduleSlotDto GetSlot(SlotDto slot)
    {
        logger.Log($"Slot: Start={slot.Start}, End={slot.End}, Type={slot.Type}");

        TimeOnly start = new(slot.Start / 60, slot.Start % 60);
        TimeOnly end = slot.End == 1440
            ? new TimeOnly(23, 59, 59, 999)
            : new TimeOnly(slot.End / 60, slot.End % 60);
        bool isEnabled = slot.Type == "Definite";

        return new ScheduleSlotDto { Start = start, End = end, IsEnabled = isEnabled };
    }

    private async Task<(int cityId, int dsosId)> GetRegionData(string city, string dsos)
    {
        await RandomDelay();

        var regions = await api.Get<RegionDto[]>($"{addressApiUrl}/regions");
        var region = regions!.First(r => r.Value == city);
        var cityId = region.Id;
        var dsosId = region.Dsos.First(d => d.Name.Contains(dsos, StringComparison.CurrentCultureIgnoreCase)).Id;

        return (cityId, dsosId);
    }

    private async Task<int> GetStreetId(string street, int cityId, int dsosId)
    {
        await RandomDelay();

        var streets = await api.Get<BaseDto[]>($"{addressApiUrl}/streets?regionId={cityId}&query={street}&dsoId={dsosId}");
        var streetId = streets!.First(d => d.Value.Contains(street, StringComparison.CurrentCultureIgnoreCase)).Id;

        return streetId;
    }

    private async Task<int> GetHouseId(string house, int cityId, int dsosId, int streetId)
    {
        await RandomDelay();

        var houses = await api.Get<BaseDto[]>($"{addressApiUrl}/houses?regionId={cityId}&streetId={streetId}&query=&dsoId={dsosId}");
        var houseId = houses!.First(d => d.Value.Contains(house, StringComparison.CurrentCultureIgnoreCase)).Id;

        return houseId;
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

    private static async Task RandomDelay()
    {
        int delayMilliseconds = Random.Shared.Next(500, 2500);
        await Task.Delay(delayMilliseconds);
    }
}
