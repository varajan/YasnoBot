using Base.DTOs.Schedule;

namespace Base.Interfaces;

public interface IScheduleService
{
    Task<TwoDayScheduleDto> GetNewSchedule();
    TwoDayScheduleDto GetCurrent();
    void SetCurrent(TwoDayScheduleDto schedule);

    Task<List<string>> GetCities(int waitTimeout = 0);
    Task<List<string>> GetDsoses(int waitTimeout = 0);
    Task<List<string>> GetHouses(int waitTimeout = 0);

    Task<(int cityId, int dsosId)> GetRegionData(string city, string dsos, int waitTimeout = 0);
    Task<int> GetStreetId(string street, int cityId, int dsosId, int waitTimeout = 0);
    Task<int> GetHouseId(string house, int cityId, int dsosId, int streetId, int waitTimeout = 0);
}
