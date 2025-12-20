using Backend.Api.Data;
using Backend.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class PreferenceService
{
    private readonly AppDbContext _db;

    public PreferenceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeePreference>> GetByScheduleIdAsync(Guid scheduleId, CancellationToken ct = default)
    {
        return await (
            from preference in _db.EmployeePreferences
            join shift in _db.Shifts on preference.ShiftId equals shift.Id
            where shift.ShiftScheduleId == scheduleId
            select preference
        ).AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<EmployeePreference>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct = default)
    {
        return await _db.EmployeePreferences
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .ToListAsync(ct);
    }

    public async Task<EmployeePreference?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.EmployeePreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<EmployeePreference> CreateAsync(EmployeePreference preference, CancellationToken ct = default)
    {
        preference.Id = Guid.NewGuid();
        preference.CreatedAt = DateTime.UtcNow;

        _db.EmployeePreferences.Add(preference);
        await _db.SaveChangesAsync(ct);

        return preference;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.EmployeePreferences.FindAsync([id], ct);
        if (entity is not null)
        {
            _db.EmployeePreferences.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
