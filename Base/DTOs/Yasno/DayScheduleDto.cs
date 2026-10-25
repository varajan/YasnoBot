using System.Text.Json.Serialization;

namespace Base.DTOs.Yasno;

public class DayScheduleDto
{
    [JsonPropertyName("slots")]
    public List<SlotDto> Slots { get; set; } = new();

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
