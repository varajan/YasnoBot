using System.Text.Json.Serialization;

namespace Base.DTOs.Yasno;

public class DsoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
