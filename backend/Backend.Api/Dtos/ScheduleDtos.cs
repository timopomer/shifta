using Backend.Api.Entities;

namespace Backend.Api.Dtos;

public record CreateScheduleRequest
{
    public required string Name { get; init; }
    public required DateTime WeekStartDate { get; init; }
}

public record ScheduleResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime WeekStartDate { get; init; }
    public required ScheduleStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public DateTime? FinalizedAt { get; init; }
}

public record ScheduleDetailResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime WeekStartDate { get; init; }
    public required ScheduleStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public DateTime? FinalizedAt { get; init; }
    public required List<ShiftResponse> Shifts { get; init; }
    public required List<ShiftAssignmentResponse> Assignments { get; init; }
}
