using Base;
using Base.DTOs;
using Base.Extensions;
using Base.Interfaces;
using YasnoLogger.Interfaces;

namespace Service;

public class Service
{
    static async Task Main()
    {
        ServicesProvider.AddApplicationServices();

        var scheduleService = ServicesProvider.Get<IScheduleService>();
        var historyService = ServicesProvider.Get<IHistoryService>();
        var statusService = ServicesProvider.Get<IStatusService>();
        var logger = ServicesProvider.Get<ILogger>();

        var count = 0;

        while (true)
        {
            count++;
            if (count % 15 == 0)
            {
                var newSchedule = await scheduleService.GetNewSchedule();
                var currentSchedule = scheduleService.GetCurrent();

                if (currentSchedule is null || !currentSchedule.IsEqual(newSchedule))
                {
                    logger.Log("Updating schedule.");
                    scheduleService.SetCurrent(newSchedule);

                    if (currentSchedule?.Tomorrow?.Date == newSchedule.Today?.Date)
                    {
                        var stateService = ServicesProvider.Get<IBotStateService>();
                        stateService.MarkAsNotified(newSchedule);
                    }
                }
            }

            var currentStatus = statusService.GetCurrentStatus();
            var lastStatus = historyService.GetLastRecord()?.Status ?? Status.Blackout;

            if (lastStatus != currentStatus)
            {
                historyService.AddRecord(currentStatus);
                logger.Log($"Status changed: {lastStatus} -> {currentStatus}");
            }

            var timeout = DateTime.Now.TillNextMinute();
            Thread.Sleep(timeout);
        }
    }
}
