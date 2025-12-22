namespace Backend.Api.Entities;

/// <summary>
/// Represents a request from an employee to work or not work a specific shift.
/// Must be approved by a manager.
/// </summary>
public class ShiftRequest
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ShiftId { get; set; }
    public ShiftRequestType RequestType { get; set; }
    public RequestStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

