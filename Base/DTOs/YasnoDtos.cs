using System.Text.Json.Serialization;

namespace Base.DTOs;

public class RegionDto : BaseDto
{
    [JsonPropertyName("hasCities")]
    public bool HasCities { get; set; }

    [JsonPropertyName("dsos")]
    public List<DsoDto> Dsos { get; set; } = new();
}

public class DsoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class BaseDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class GroupDto
{
    [JsonPropertyName("group")]
    public int Group { get; set; }

    [JsonPropertyName("subgroup")]
    public int Subgroup { get; set; }
}

public class ScheduleResponse : Dictionary<string, GroupScheduleDto>
{
}

public class GroupScheduleDto
{
    [JsonPropertyName("today")]
    public DayScheduleDto Today { get; set; } = new();

    [JsonPropertyName("tomorrow")]
    public DayScheduleDto Tomorrow { get; set; } = new();

    [JsonPropertyName("updatedOn")]
    public DateTimeOffset UpdatedOn { get; set; }
}

public class DayScheduleDto
{
    [JsonPropertyName("slots")]
    public List<SlotDto> Slots { get; set; } = new();

    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class SlotDto
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}