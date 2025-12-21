namespace Backend.Api.Entities;

public class Shift
{
    public Guid Id { get; set; }
    public Guid ShiftScheduleId { get; set; }
    public required string Name { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<string> RequiredAbilities { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
