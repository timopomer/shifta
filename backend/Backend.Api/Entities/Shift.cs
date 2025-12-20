namespace Backend.Api.Entities;

/// <summary>
/// Represents a single shift within a schedule.
/// </summary>
public class Shift
{
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the parent schedule.
    /// </summary>
    public Guid ShiftScheduleId { get; set; }

    public required string Name { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    /// <summary>
    /// List of required abilities/skills for this shift (e.g., "bartender").
    /// Stored as a JSON array in the database.
    /// </summary>
    public List<string> RequiredAbilities { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
