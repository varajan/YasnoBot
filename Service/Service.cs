using Base;
using Base.Extensions;
using Base.Interfaces;
using YasnoLogger.Interfaces;

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
                }
            }

            var currentStatus = statusService.IsRunning();
            var lastStatus = historyService.GetLastRecord()?.Status ?? !currentStatus;

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
