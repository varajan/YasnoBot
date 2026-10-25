using System.Text.Json.Serialization;

namespace Base.DTOs.Yasno;

public class RegionDto : BaseDto
{
    [JsonPropertyName("hasCities")]
    public bool HasCities { get; set; }

    [JsonPropertyName("dsos")]
    public List<DsoDto> Dsos { get; set; } = new();
}
