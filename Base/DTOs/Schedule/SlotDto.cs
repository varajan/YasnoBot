namespace Base.DTOs.Schedule;

public class SlotDto
{
    public required TimeOnly Start { get; set; }
    public required TimeOnly End { get; set; }
    public required Status Status { get; set; }
}
