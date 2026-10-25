using System.Text.Json.Serialization;

namespace Base.DTOs.Yasno;

public class BaseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
