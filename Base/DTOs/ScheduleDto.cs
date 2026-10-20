namespace Base.DTOs;

public class TwoDayScheduleDto
{
    public ScheduleDto Today { get; set; }
    public ScheduleDto Tomorrow { get; set; }
}

public class ScheduleDto
{
    public ScheduleSlotDto[] Slots { get; set; }
}

public class ScheduleSlotDto
{
    public required TimeOnly Start { get; set; }
    public required TimeOnly End { get; set; }
    public required bool IsEnabled { get; set; }
}
