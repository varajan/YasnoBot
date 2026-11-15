using Base.DTOs.Schedule;

namespace Base.Interfaces;

public interface IScheduleService
{
    Task<TwoDayScheduleDto> GetNewSchedule();
    TwoDayScheduleDto GetCurrent();
    void SetCurrent(TwoDayScheduleDto schedule);

    Task<List<string>> GetCities();
    Task<List<string>> GetDsoses();
    Task<List<string>> GetHuoses();

    Task<(int cityId, int dsosId)> GetRegionData(string city, string dsos);
    Task<int> GetStreetId(string street, int cityId, int dsosId);
    Task<int> GetHouseId(string house, int cityId, int dsosId, int streetId);
}
