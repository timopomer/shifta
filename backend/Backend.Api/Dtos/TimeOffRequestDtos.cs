using Backend.Api.Entities;

namespace Backend.Api.Dtos;

public record CreateTimeOffRequestRequest
{
    public required Guid EmployeeId { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public string? Reason { get; init; }
}

public record ReviewTimeOffRequestRequest
{
    public required RequestStatus Status { get; init; }
    public string? ReviewNote { get; init; }
}

public record TimeOffRequestResponse
{
    public required Guid Id { get; init; }
    public required Guid EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required RequestStatus Status { get; init; }
    public string? Reason { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public Guid? ReviewedById { get; init; }
    public string? ReviewedByName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewNote { get; init; }
}

