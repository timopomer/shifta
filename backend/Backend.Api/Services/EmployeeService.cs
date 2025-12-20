using Backend.Api.Data;
using Backend.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class EmployeeService
{
    private readonly AppDbContext _db;

    public EmployeeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Employee>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Employees
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(ct);
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<Employee> CreateAsync(Employee employee, CancellationToken ct = default)
    {
        employee.Id = Guid.NewGuid();
        employee.CreatedAt = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(ct);

        return employee;
    }

    public async Task<Employee> UpdateAsync(Employee employee, CancellationToken ct = default)
    {
        employee.UpdatedAt = DateTime.UtcNow;

        _db.Employees.Update(employee);
        await _db.SaveChangesAsync(ct);

        return employee;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Employees.FindAsync([id], ct);
        if (entity is not null)
        {
            _db.Employees.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
