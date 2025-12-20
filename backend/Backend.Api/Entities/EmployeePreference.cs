namespace Backend.Api.Entities;

/// <summary>
/// Type of preference an employee can express.
/// </summary>
public enum PreferenceType
{
    /// <summary>
    /// Employee prefers to work this specific shift.
    /// </summary>
    PreferShift = 0,

    /// <summary>
    /// Employee prefers to work during a specific time period.
    /// </summary>
    PreferPeriod = 1,

    /// <summary>
    /// Employee is unavailable during a specific time period.
    /// </summary>
    Unavailable = 2
}

/// <summary>
/// Represents an employee's preference for a shift or time period.
/// </summary>
public class EmployeePreference
{
    public Guid Id { get; set; }

    /// <summary>
    /// The employee expressing this preference.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The shift this preference relates to.
    /// For PreferShift type, this is the specific shift.
    /// For period-based preferences, this links to a shift in the schedule.
    /// </summary>
    public Guid ShiftId { get; set; }

    /// <summary>
    /// Type of preference (PreferShift, PreferPeriod, Unavailable).
    /// </summary>
    public PreferenceType Type { get; set; }

    /// <summary>
    /// For period-based preferences: start of the preferred/unavailable period.
    /// </summary>
    public DateTime? PeriodStart { get; set; }

    /// <summary>
    /// For period-based preferences: end of the preferred/unavailable period.
    /// </summary>
    public DateTime? PeriodEnd { get; set; }

    /// <summary>
    /// Whether this is a hard constraint (must be satisfied) or soft (prefer but not required).
    /// </summary>
    public bool IsHard { get; set; }

    public DateTime CreatedAt { get; set; }
}
