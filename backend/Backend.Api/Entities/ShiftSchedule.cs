namespace Backend.Api.Entities;

public class ShiftSchedule
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime WeekStartDate { get; set; }
    public ScheduleStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
}
