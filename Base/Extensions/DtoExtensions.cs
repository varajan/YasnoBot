using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Base.Extensions;

public static class DtoExtensions
{
    public static Guid GetGuid(this object obj)
    {
        string json = obj.Serialize()!;
        byte[] inputBytes = Encoding.UTF8.GetBytes(json);
        using var provider = MD5.Create();
        byte[] hashBytes = provider.ComputeHash(inputBytes);
        return new Guid(hashBytes);
    }

    public static bool IsEqual(this object dtoA, object dtoB) =>
        dtoA.Serialize() == dtoB.Serialize();

    public static T? DeSerialize<T>(this string json) where T : class
    {
        try
        {
            return json is null ? null : JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    public static string? Serialize(this object dto) => dto is null ? null : JsonSerializer.Serialize(dto);
}
