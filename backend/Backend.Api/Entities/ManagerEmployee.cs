namespace Backend.Api.Entities;

/// <summary>
/// Many-to-many relationship between managers and employees.
/// An employee can have multiple managers, and a manager can manage multiple employees.
/// </summary>
public class ManagerEmployee
{
    public Guid Id { get; set; }
    public Guid ManagerId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

