using Base.Interfaces;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using YasnoLogger.Interfaces;

namespace Base.Services;

public class StatusService(ILogger logger) : IStatusService
{
    public bool IsRunning()
    {
        var targetMacs = new List<string>()
        {
            "4a:23:34:18:f9:2a"
        };
        //var targetMacs = new List<string>()
        //{
        //    "50:5a:65:95:94:76",
        //    "e8:fb:1c:4a:49:65",
        //    "2c:6f:c9:08:f1:07"
        //};

        UpdateArpTable().Wait();
        var activeMacs = GetActiveMacAddressesFromArp();
        var normalizedTargets = targetMacs.Select(m => m.Trim().ToLower()).ToHashSet();
        var isRunning = normalizedTargets.All(activeMacs.Contains);
        logger.Log($"IsRunning check: {isRunning}. MACs: {string.Join(", ", targetMacs)}");

        return isRunning;
    }

    private async Task UpdateArpTable()
    {
        var aprTable = CallArpCommand();
        var localIp = aprTable.Trim().Split(' ')[1];
        var localIpParts = localIp.Split('.').Take(3);
        var subnetPrefix = string.Join('.', localIpParts);

        var tasks = Enumerable.Range(1, 254).Select(async i =>
        {
            string ip = $"{subnetPrefix}.{i}";
            using var ping = new Ping();
            try
            {
                await ping.SendPingAsync(ip, 500);
            }
            catch { }
        });

        await Task.WhenAll(tasks);
    }

    private HashSet<string> GetActiveMacAddressesFromArp()
    {
        var macs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string output = CallArpCommand();
            var matches = Regex.Matches(output, "([0-9a-fA-F]{2}[:-]){5}[0-9a-fA-F]{2}");

            foreach (Match match in matches)
            {
                string mac = match.Value.ToLower().Replace("-", ":");
                macs.Add(mac);
            }
        }
        catch (Exception ex)
        {
            logger.Log(ex);
        }

        return macs;
    }

    private string CallArpCommand()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "arp",
                Arguments = "-a",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            using var reader = process.StandardOutput;
            string output = reader.ReadToEnd();
            process.WaitForExit();

            return output;
        }
        catch (Exception ex)
        {
            logger.Log(ex);
            return string.Empty;
        }
    }
}
