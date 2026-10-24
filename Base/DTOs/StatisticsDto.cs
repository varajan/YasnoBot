namespace Base.DTOs;

public class StatisticsDto
{
    public DateTime DateTime { get; set; }
    public Status Status { get; set; }
}

public enum Status
{
    Blackout = 0,
    PowerOn
}
