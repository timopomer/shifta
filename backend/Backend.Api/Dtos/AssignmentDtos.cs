namespace Backend.Api.Dtos;

public record ShiftAssignmentResponse
{
    public required Guid Id { get; init; }
    public required Guid ShiftId { get; init; }
    public required Guid EmployeeId { get; init; }
    public required DateTime AssignedAt { get; init; }
}
