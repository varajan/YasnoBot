using System.Globalization;
using System.Text.RegularExpressions;

namespace YasnoBot.YasnoLogger.Extensions;

public static class StringExtensions
{
    public static string NullIfEmpty(this string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        return line;
    }

    public static string ReplaceWhitespaces(this string line) => new Regex(@"\s+").Replace(line, string.Empty);

    public static int AsCurrency(this string value) => value.Replace("$", "").Replace(",", "").ReplaceWhitespaces().ToInt();
    public static string AsCurrency(this long value) => (value >= 0 ? "$" : "-$") + Math.Abs(value).ToString("n0", CultureInfo.InvariantCulture);
    public static string AsCurrency(this int value) => (value >= 0 ? "$" : "-$") + Math.Abs(value).ToString("n0", CultureInfo.InvariantCulture);

    public static string SubStringTo(this string line, string to) => line.Contains(to) ? line.Split(to).First() : line;

    public static string SubString(this string line, string from, string to)
    {
        var start = line.IndexOf(from, StringComparison.OrdinalIgnoreCase) + from.Length;
        var count = line.IndexOf(to, Math.Min(start, line.Length), StringComparison.OrdinalIgnoreCase) - start;

        return line.IndexOf(from, StringComparison.OrdinalIgnoreCase) >= 0 ? line.Substring(start, count) : string.Empty;
    }

    public static string SubString(this string line, string from)
    {
        var start = line.ContainsIgnoreCase(from)
            ? line.IndexOf(from, StringComparison.OrdinalIgnoreCase) + from.Length
            : line.Length;

        return line.Substring(start);
    }

    public static string Escape(this string line) => line
        .Replace("_", "\\_")
        .Replace("\r\n", "\n")
        .Replace("�", "•")
        .Replace("\u00F7", "÷");

    public static bool ContainsIgnoreCase(this string line, string word) => line.Contains(word ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    public static T ParseEnum<T>(this string value) => (T)Enum.Parse(typeof(T), value, true);

    public static long ToLong(this string number)
    {
        try
        {
            return long.Parse(number);
        }
        catch
        {
            return 0;
        }
    }

    public static int ToInt(this string number)
    {
        try
        {
            return int.Parse(number.Replace(" ", "").Trim());
        }
        catch
        {
            return 0;
        }
    }

    public static decimal ToDecimal(this string number)
    {
        try
        {
            return decimal.Parse(number, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }

    public static DateTime ToDateTime(this string date) => DateTime.Parse(date);
    public static string AsString(this DateTime dateTime, string format = "yyyy.MM.dd HH:mm") => dateTime.ToString(format, CultureInfo.InvariantCulture);

    public static int GetApartmentsCount(this string title)
    {
        int result = 1;

        if (title.ToLower().Contains("plex"))
        {
            result = title.ToLower().Replace("-plex", "").Replace("*", "").Split("x")
                .Select(x => x.ToInt())
                .Aggregate((x, r) => x * r);
        }

        return result;
    }
}