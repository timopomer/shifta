namespace Backend.Api.Dtos;

public record CreateEmployeeRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public List<string> Abilities { get; init; } = [];
    public bool IsManager { get; init; }
}

public record UpdateEmployeeRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public List<string> Abilities { get; init; } = [];
    public bool IsManager { get; init; }
}

public record EmployeeResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required List<string> Abilities { get; init; }
    public required bool IsManager { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
