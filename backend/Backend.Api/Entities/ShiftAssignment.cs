namespace Backend.Api.Entities;

public class ShiftAssignment
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime AssignedAt { get; set; }
}
