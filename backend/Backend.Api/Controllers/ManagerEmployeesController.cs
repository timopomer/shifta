using Backend.Api.Data;
using Backend.Api.Dtos;
using Backend.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/manager-employees")]
public class ManagerEmployeesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ManagerEmployeesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ManagerEmployeeResponse>>> GetAll(CancellationToken ct)
    {
        var relationships = await (
            from me in _db.ManagerEmployees
            join manager in _db.Employees on me.ManagerId equals manager.Id
            join employee in _db.Employees on me.EmployeeId equals employee.Id
            select new ManagerEmployeeResponse
            {
                Id = me.Id,
                ManagerId = me.ManagerId,
                EmployeeId = me.EmployeeId,
                ManagerName = manager.Name,
                EmployeeName = employee.Name,
                CreatedAt = me.CreatedAt
            }
        ).AsNoTracking().ToListAsync(ct);

        return relationships;
    }

    [HttpGet("by-manager/{managerId:guid}")]
    public async Task<ActionResult<ManagerWithEmployeesResponse>> GetByManager(Guid managerId, CancellationToken ct)
    {
        var manager = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == managerId, ct);

        if (manager is null)
            return NotFound(new { error = "Manager not found" });

        var employees = await (
            from me in _db.ManagerEmployees
            join employee in _db.Employees on me.EmployeeId equals employee.Id
            where me.ManagerId == managerId
            select new EmployeeBasicInfo
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email
            }
        ).AsNoTracking().ToListAsync(ct);

        return new ManagerWithEmployeesResponse
        {
            ManagerId = managerId,
            ManagerName = manager.Name,
            Employees = employees
        };
    }

    [HttpGet("by-employee/{employeeId:guid}")]
    public async Task<ActionResult<EmployeeWithManagersResponse>> GetByEmployee(Guid employeeId, CancellationToken ct)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);

        if (employee is null)
            return NotFound(new { error = "Employee not found" });

        var managers = await (
            from me in _db.ManagerEmployees
            join manager in _db.Employees on me.ManagerId equals manager.Id
            where me.EmployeeId == employeeId
            select new EmployeeBasicInfo
            {
                Id = manager.Id,
                Name = manager.Name,
                Email = manager.Email
            }
        ).AsNoTracking().ToListAsync(ct);

        return new EmployeeWithManagersResponse
        {
            EmployeeId = employeeId,
            EmployeeName = employee.Name,
            Managers = managers
        };
    }

    [HttpPost]
    public async Task<ActionResult<ManagerEmployeeResponse>> Create(CreateManagerEmployeeRequest request, CancellationToken ct)
    {
        // Validate manager exists and is a manager
        var manager = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.ManagerId, ct);

        if (manager is null)
            return BadRequest(new { error = "Manager not found" });

        if (!manager.IsManager)
            return BadRequest(new { error = "The specified user is not a manager" });

        // Validate employee exists
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);

        if (employee is null)
            return BadRequest(new { error = "Employee not found" });

        // Check for duplicate
        var exists = await _db.ManagerEmployees
            .AnyAsync(me => me.ManagerId == request.ManagerId && me.EmployeeId == request.EmployeeId, ct);

        if (exists)
            return BadRequest(new { error = "This manager-employee relationship already exists" });

        var relationship = new ManagerEmployee
        {
            Id = Guid.NewGuid(),
            ManagerId = request.ManagerId,
            EmployeeId = request.EmployeeId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ManagerEmployees.Add(relationship);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new ManagerEmployeeResponse
        {
            Id = relationship.Id,
            ManagerId = relationship.ManagerId,
            EmployeeId = relationship.EmployeeId,
            ManagerName = manager.Name,
            EmployeeName = employee.Name,
            CreatedAt = relationship.CreatedAt
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var relationship = await _db.ManagerEmployees.FindAsync([id], ct);
        if (relationship is null)
            return NotFound();

        _db.ManagerEmployees.Remove(relationship);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

