using Backend.Api.Data;
using Backend.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class ShiftService
{
    private readonly AppDbContext _db;

    public ShiftService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Shift>> GetByScheduleIdAsync(Guid scheduleId, CancellationToken ct = default)
    {
        return await _db.Shifts
            .AsNoTracking()
            .Where(s => s.ShiftScheduleId == scheduleId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<Shift?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Shift> CreateAsync(Shift shift, CancellationToken ct = default)
    {
        shift.Id = Guid.NewGuid();
        shift.CreatedAt = DateTime.UtcNow;
        shift.UpdatedAt = DateTime.UtcNow;

        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync(ct);

        return shift;
    }

    public async Task<Shift> UpdateAsync(Shift shift, CancellationToken ct = default)
    {
        shift.UpdatedAt = DateTime.UtcNow;

        _db.Shifts.Update(shift);
        await _db.SaveChangesAsync(ct);

        return shift;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Shifts.FindAsync([id], ct);
        if (entity is not null)
        {
            _db.Shifts.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
