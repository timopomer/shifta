namespace Backend.Api.Entities;

/// <summary>
/// Represents the lifecycle states of a shift schedule.
/// </summary>
public enum ScheduleStatus
{
    /// <summary>
    /// Initial state. Schedule is being created, shifts can be added/modified.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Schedule is open for employees to submit their preferences.
    /// </summary>
    OpenForPreferences = 1,

    /// <summary>
    /// Preferences are closed. Manager can review and trigger optimization.
    /// </summary>
    PendingReview = 2,

    /// <summary>
    /// Optimization has been run and assignments are final.
    /// </summary>
    Finalized = 3,

    /// <summary>
    /// Schedule period has passed. Kept for historical records.
    /// </summary>
    Archived = 4
}

/// <summary>
/// Represents a weekly shift schedule containing multiple shifts.
/// </summary>
public class ShiftSchedule
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// The start date of the week this schedule covers.
    /// </summary>
    public DateTime WeekStartDate { get; set; }

    /// <summary>
    /// Current status in the schedule lifecycle.
    /// </summary>
    public ScheduleStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the schedule was finalized (optimization completed).
    /// </summary>
    public DateTime? FinalizedAt { get; set; }
}
