using Backend.Api.Dtos;
using Backend.Api.Entities;
using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _employeeService;

    public EmployeesController(EmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmployeeResponse>>> GetAll(CancellationToken ct)
    {
        var employees = await _employeeService.GetAllAsync(ct);
        return employees.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> GetById(Guid id, CancellationToken ct)
    {
        var employee = await _employeeService.GetByIdAsync(id, ct);
        if (employee is null)
            return NotFound();

        return MapToResponse(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create(CreateEmployeeRequest request, CancellationToken ct)
    {
        var employee = new Employee
        {
            Name = request.Name,
            Email = request.Email,
            Abilities = request.Abilities,
            IsManager = request.IsManager
        };

        var created = await _employeeService.CreateAsync(employee, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToResponse(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> Update(Guid id, UpdateEmployeeRequest request, CancellationToken ct)
    {
        var existing = await _employeeService.GetByIdAsync(id, ct);
        if (existing is null)
            return NotFound();

        existing.Name = request.Name;
        existing.Email = request.Email;
        existing.Abilities = request.Abilities;
        existing.IsManager = request.IsManager;

        var updated = await _employeeService.UpdateAsync(existing, ct);
        return MapToResponse(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _employeeService.DeleteAsync(id, ct);
        return NoContent();
    }

    private static EmployeeResponse MapToResponse(Employee employee) => new()
    {
        Id = employee.Id,
        Name = employee.Name,
        Email = employee.Email,
        Abilities = employee.Abilities,
        IsManager = employee.IsManager,
        CreatedAt = employee.CreatedAt,
        UpdatedAt = employee.UpdatedAt
    };
}
