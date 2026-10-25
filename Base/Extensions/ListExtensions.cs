namespace Base.Extensions;

public static class ListExtensions
{
    public static IEnumerable<int> ToInt(this IEnumerable<string> list) => list.Select(x => x.ToInt());
    public static IEnumerable<long> ToLong(this IEnumerable<string> list) => list.Select(x => x.ToLong());
    public static IEnumerable<string> AsCurrency(this IEnumerable<string> list) => list.ToInt().AsCurrency();
    public static IEnumerable<string> AsCurrency(this IEnumerable<int> list) => list.Select(x => x.AsCurrency());
    public static T Random<T>(this IEnumerable<T> list) => list.Random(1).FirstOrDefault();
    public static IEnumerable<string> Trim(this IEnumerable<string> list) => list.Select(x => x.Trim());
    public static List<T> Random<T>(this IEnumerable<T> list, int count) => list.Shuffle().Take(count).ToList();
    public static string Join(this IEnumerable<string> list, string separator) => string.Join(separator, list);
    public static bool ContainsAny<T>(this IEnumerable<T> source, params T[] values) => source.Any(values.Contains);
}