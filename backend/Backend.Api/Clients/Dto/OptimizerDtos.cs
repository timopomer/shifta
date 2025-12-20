using System.Text.Json.Serialization;

namespace Backend.Api.Clients.Dto;

// Request DTOs

public record PreferenceDto
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("shift_id")]
    public string? ShiftId { get; init; }

    [JsonPropertyName("start")]
    public DateTime? Start { get; init; }

    [JsonPropertyName("end")]
    public DateTime? End { get; init; }

    [JsonPropertyName("is_hard")]
    public bool IsHard { get; init; }
}

public record EmployeeDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("abilities")]
    public List<string> Abilities { get; init; } = [];

    [JsonPropertyName("preferences")]
    public List<PreferenceDto> Preferences { get; init; } = [];
}

public record ShiftDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("start_time")]
    public required DateTime StartTime { get; init; }

    [JsonPropertyName("end_time")]
    public required DateTime EndTime { get; init; }

    [JsonPropertyName("required_abilities")]
    public List<string> RequiredAbilities { get; init; } = [];
}

public record OptimizeRequest
{
    [JsonPropertyName("employees")]
    public required List<EmployeeDto> Employees { get; init; }

    [JsonPropertyName("shifts")]
    public required List<ShiftDto> Shifts { get; init; }

    [JsonPropertyName("max_solutions")]
    public int MaxSolutions { get; init; } = 1;
}

// Response DTOs

public record SolutionMetricsDto
{
    [JsonPropertyName("soft_preference_score")]
    public int SoftPreferenceScore { get; init; }

    [JsonPropertyName("fairness_score")]
    public double FairnessScore { get; init; }

    [JsonPropertyName("preferences_satisfied")]
    public Dictionary<string, int> PreferencesSatisfied { get; init; } = [];

    [JsonPropertyName("total_shifts_assigned")]
    public int TotalShiftsAssigned { get; init; }
}

public record SolutionDto
{
    [JsonPropertyName("assignments")]
    public required Dictionary<string, string> Assignments { get; init; }

    [JsonPropertyName("metrics")]
    public required SolutionMetricsDto Metrics { get; init; }
}

public record OptimizeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("solutions")]
    public List<SolutionDto> Solutions { get; init; } = [];

    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
