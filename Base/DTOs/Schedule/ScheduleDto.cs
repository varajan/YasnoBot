namespace Base.DTOs.Schedule;

public class ScheduleDto
{
    public DateTime Date { get; set; }
    public bool IsEmergence { get; set; }
    public SlotDto[] Slots { get; set; }
}
