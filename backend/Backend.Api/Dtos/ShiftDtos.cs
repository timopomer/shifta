namespace Backend.Api.Dtos;

public record CreateShiftRequest
{
    public required string Name { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public List<string> RequiredAbilities { get; init; } = [];
}

public record UpdateShiftRequest
{
    public required string Name { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public List<string> RequiredAbilities { get; init; } = [];
}

public record ShiftResponse
{
    public required Guid Id { get; init; }
    public required Guid ShiftScheduleId { get; init; }
    public required string Name { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required List<string> RequiredAbilities { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
