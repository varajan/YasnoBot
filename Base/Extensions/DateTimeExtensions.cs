namespace Base.Extensions;

public static class DateTimeExtensions
{
    public static int TillNextMinute(this DateTime dateTime)
    {
        var nextMinute = dateTime.AddMinutes(1).AddSeconds(-dateTime.Second).AddMilliseconds(-dateTime.Millisecond);
        int delay = (int)(nextMinute - dateTime).TotalMilliseconds;

        return delay;
    }

    public static string GetTimeDifference(this DateTime dateFrom, DateTime? dateTo = null)
    {
        TimeSpan diff = (dateFrom - (dateTo ?? DateTime.Now)).Duration();

        var parts = new List<string>();

        if (diff.Days > 0)
            parts.Add($"{diff.Days} {GetDeclension(diff.Days, "день", "дні", "днів")}");

        if (diff.Hours > 0)
            parts.Add($"{diff.Hours} {GetDeclension(diff.Hours, "година", "години", "годин")}");

        if (diff.Minutes > 0)
            parts.Add($"{diff.Minutes} {GetDeclension(diff.Minutes, "хвилина", "хвилини", "хвилин")}");

        return parts.Count > 0 ? string.Join(", ", parts) : "менше хвилини";
    }

    private static string GetDeclension(int number, string form1, string form2, string form5)
    {
        int n = Math.Abs(number) % 100;
        int n1 = n % 10;

        if (n > 10 && n < 20) return form5;
        if (n1 > 1 && n1 < 5) return form2;
        if (n1 == 1) return form1;

        return form5;
    }
}
