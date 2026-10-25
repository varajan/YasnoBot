using System.Text.Json.Serialization;

namespace Base.DTOs.Yasno;

public class GroupScheduleDto
{
    [JsonPropertyName("today")]
    public DayScheduleDto Today { get; set; } = new();

    [JsonPropertyName("tomorrow")]
    public DayScheduleDto Tomorrow { get; set; } = new();

    [JsonPropertyName("updatedOn")]
    public DateTimeOffset UpdatedOn { get; set; }
}
