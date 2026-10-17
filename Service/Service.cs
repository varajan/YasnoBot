using Base;
using Base.Extensions;
using Base.Interfaces;
using YasnoLogger.Interfaces;

public class Service
{
    static async Task Main()
    {
        ServicesProvider.AddApplicationServices();

        var historyService = ServicesProvider.Get<IHistoryService>();
        var statusService = ServicesProvider.Get<IStatusService>();
        var logger = ServicesProvider.Get<ILogger>();

        while (true)
        {
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
