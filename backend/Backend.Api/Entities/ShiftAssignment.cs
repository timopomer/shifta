namespace Backend.Api.Entities;

/// <summary>
/// Represents the final assignment of an employee to a shift after optimization.
/// </summary>
public class ShiftAssignment
{
    public Guid Id { get; set; }

    /// <summary>
    /// The shift being assigned.
    /// </summary>
    public Guid ShiftId { get; set; }

    /// <summary>
    /// The employee assigned to this shift.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// When the assignment was created (when schedule was finalized).
    /// </summary>
    public DateTime AssignedAt { get; set; }
}
