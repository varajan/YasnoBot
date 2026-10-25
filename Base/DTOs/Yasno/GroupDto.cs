using System.Text.Json.Serialization;

namespace Base.DTOs.Yasno;

public class GroupDto
{
    [JsonPropertyName("group")]
    public int Group { get; set; }

    [JsonPropertyName("subgroup")]
    public int Subgroup { get; set; }
}
