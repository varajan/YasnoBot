using Base.DTOs.Schedule;
using Base.Interfaces;
using System.Net.NetworkInformation;
using YasnoLogger.Interfaces;

namespace Base.Services;

public class StatusService(ILogger logger) : IStatusService
{
    public Status GetCurrentStatus()
    {
        var settings = ServicesProvider.Get<ISettings>();
        var areHostsUp = false;
        var targetIPs = settings.Get("IP").Split(',').Select(m => m.Trim());

        for (var i = 1; i <= 3; i++)
        {
            areHostsUp = targetIPs.Any(IsHostUp);
            if (areHostsUp) break;

            Thread.Sleep(i * 2_000);
        }

        return areHostsUp ? Status.PowerOn : Status.Blackout;
    }

    private bool IsHostUp(string ip)
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send(ip, 2_000);
            return reply.Status == IPStatus.Success;
        }
        catch (Exception ex)
        {
            logger.Log(ex);
            return false;
        }
    }
}
