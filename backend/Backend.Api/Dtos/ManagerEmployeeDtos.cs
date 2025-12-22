namespace Backend.Api.Dtos;

public record CreateManagerEmployeeRequest
{
    public required Guid ManagerId { get; init; }
    public required Guid EmployeeId { get; init; }
}

public record ManagerEmployeeResponse
{
    public required Guid Id { get; init; }
    public required Guid ManagerId { get; init; }
    public required Guid EmployeeId { get; init; }
    public required string ManagerName { get; init; }
    public required string EmployeeName { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record ManagerWithEmployeesResponse
{
    public required Guid ManagerId { get; init; }
    public required string ManagerName { get; init; }
    public required List<EmployeeBasicInfo> Employees { get; init; }
}

public record EmployeeWithManagersResponse
{
    public required Guid EmployeeId { get; init; }
    public required string EmployeeName { get; init; }
    public required List<EmployeeBasicInfo> Managers { get; init; }
}

public record EmployeeBasicInfo
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
}

