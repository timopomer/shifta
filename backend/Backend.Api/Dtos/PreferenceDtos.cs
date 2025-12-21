using Backend.Api.Entities;

namespace Backend.Api.Dtos;

public record CreatePreferenceRequest
{
    public required Guid EmployeeId { get; init; }
    public required Guid ShiftId { get; init; }
    public required PreferenceType Type { get; init; }
    public DateTime? PeriodStart { get; init; }
    public DateTime? PeriodEnd { get; init; }
    public bool IsHard { get; init; }
}

public record PreferenceResponse
{
    public required Guid Id { get; init; }
    public required Guid EmployeeId { get; init; }
    public required Guid ShiftId { get; init; }
    public required PreferenceType Type { get; init; }
    public DateTime? PeriodStart { get; init; }
    public DateTime? PeriodEnd { get; init; }
    public required bool IsHard { get; init; }
    public required DateTime CreatedAt { get; init; }
}
