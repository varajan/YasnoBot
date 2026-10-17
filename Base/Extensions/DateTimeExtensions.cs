namespace Base.Extensions;

public static class DateTimeExtensions
{
    public static int TillNextMinute(this DateTime dateTime)
    {
        var nextMinute = dateTime.AddMinutes(1).AddSeconds(-dateTime.Second).AddMilliseconds(-dateTime.Millisecond);
        int delay = (int)(nextMinute - dateTime).TotalMilliseconds;

        return delay;
    }

    public static string GetTimeDifference(this DateTime dateTime)
    {
        TimeSpan diff = (dateTime - DateTime.Now).Duration();

        var parts = new List<string>();

        if (diff.Days > 0)
            parts.Add($"{diff.Days} {(diff.Days == 1 ? "day" : "days")}");

        if (diff.Hours > 0)
            parts.Add($"{diff.Hours} {(diff.Hours == 1 ? "hour" : "hours")}");

        if (diff.Minutes > 0)
            parts.Add($"{diff.Minutes} {(diff.Minutes == 1 ? "minute" : "minutes")}");

        if (diff.Seconds > 0)
            parts.Add($"{diff.Seconds} {(diff.Seconds == 1 ? "second" : "seconds")}");

        return parts.Count > 0 ? string.Join(", ", parts) : "0 seconds";
    }
}
