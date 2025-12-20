namespace Backend.Api.Entities;

/// <summary>
/// Represents an employee who can be assigned to shifts.
/// </summary>
public class Employee
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    /// <summary>
    /// List of abilities/skills this employee has (e.g., "bartender", "waiter", "kitchen").
    /// Stored as a JSON array in the database.
    /// </summary>
    public List<string> Abilities { get; set; } = [];

    /// <summary>
    /// Whether this employee has manager privileges.
    /// </summary>
    public bool IsManager { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
