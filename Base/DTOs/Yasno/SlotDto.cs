using System.Text.Json.Serialization;

namespace Base.DTOs.Yasno;

public class SlotDto
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}