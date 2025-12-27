using Backend.Api.Entities;

namespace Backend.Api.Dtos;

public record CreateShiftRequestRequest
{
    public required Guid EmployeeId { get; init; }
    public required Guid ShiftId { get; init; }
    public required ShiftRequestType RequestType { get; init; }
    public string? Note { get; init; }
}

public record ReviewShiftRequestRequest
{
    public required RequestStatus Status { get; init; }
    public string? ReviewNote { get; init; }
}

public record ShiftRequestResponse
{
    public required Guid Id { get; init; }
    public required Guid EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public required Guid ShiftId { get; init; }
    public required string ShiftName { get; init; }
    public required DateTime ShiftStartTime { get; init; }
    public required DateTime ShiftEndTime { get; init; }
    public required Guid ScheduleId { get; init; }
    public required string ScheduleName { get; init; }
    public required ShiftRequestType RequestType { get; init; }
    public required RequestStatus Status { get; init; }
    public string? Note { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public Guid? ReviewedById { get; init; }
    public string? ReviewedByName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewNote { get; init; }
}


