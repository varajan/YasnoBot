using System.Text.Json;

namespace Base.Extensions;

public static class DtoExtensions
{
    public static bool IsEqual(this object dtoA, object dtoB) =>
        dtoA.Serialize() == dtoB.Serialize();

    public static T? DeSerialize<T>(this string json) where T : class =>
        json is null ? null : JsonSerializer.Deserialize<T>(json);

    public static string Serialize(this object dto) => JsonSerializer.Serialize(dto);
}
