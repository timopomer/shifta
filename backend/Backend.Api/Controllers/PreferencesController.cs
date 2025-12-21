using Backend.Api.Data;
using Backend.Api.Dtos;
using Backend.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PreferencesController : ControllerBase
{
    private readonly AppDbContext _db;

    public PreferencesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("schedule/{scheduleId:guid}")]
    public async Task<ActionResult<List<PreferenceResponse>>> GetBySchedule(Guid scheduleId, CancellationToken ct)
    {
        var preferences = await (
            from preference in _db.EmployeePreferences
            join shift in _db.Shifts on preference.ShiftId equals shift.Id
            where shift.ShiftScheduleId == scheduleId
            select preference
        ).AsNoTracking().ToListAsync(ct);

        return preferences.Select(MapToResponse).ToList();
    }

    [HttpGet("employee/{employeeId:guid}")]
    public async Task<ActionResult<List<PreferenceResponse>>> GetByEmployee(Guid employeeId, CancellationToken ct)
    {
        var preferences = await _db.EmployeePreferences
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .ToListAsync(ct);

        return preferences.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PreferenceResponse>> GetById(Guid id, CancellationToken ct)
    {
        var preference = await _db.EmployeePreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (preference is null)
            return NotFound();

        return MapToResponse(preference);
    }

    [HttpPost]
    public async Task<ActionResult<PreferenceResponse>> Create(CreatePreferenceRequest request, CancellationToken ct)
    {
        // Validate the shift exists
        var shift = await _db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct);

        if (shift is null)
            return BadRequest(new { error = "Shift not found" });

        // Get the schedule to check status
        var schedule = await _db.ShiftSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shift.ShiftScheduleId, ct);

        if (schedule is null)
            return BadRequest(new { error = "Schedule not found" });

        if (schedule.Status != ScheduleStatus.OpenForPreferences)
            return BadRequest(new { error = "Preferences can only be submitted when schedule is OpenForPreferences" });

        // Validate the employee exists
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);

        if (employee is null)
            return BadRequest(new { error = "Employee not found" });

        // Validate period times for period-based preferences
        if (request.Type == PreferenceType.PreferPeriod || request.Type == PreferenceType.Unavailable)
        {
            if (!request.PeriodStart.HasValue || !request.PeriodEnd.HasValue)
                return BadRequest(new { error = "PeriodStart and PeriodEnd are required for period-based preferences" });

            if (request.PeriodEnd <= request.PeriodStart)
                return BadRequest(new { error = "PeriodEnd must be after PeriodStart" });
        }

        var preference = new EmployeePreference
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            Type = request.Type,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            IsHard = request.IsHard,
            CreatedAt = DateTime.UtcNow
        };

        _db.EmployeePreferences.Add(preference);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = preference.Id }, MapToResponse(preference));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var preference = await _db.EmployeePreferences.FindAsync([id], ct);
        if (preference is null)
            return NotFound();

        // Get the shift and schedule to check status
        var shift = await _db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == preference.ShiftId, ct);

        if (shift is not null)
        {
            var schedule = await _db.ShiftSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == shift.ShiftScheduleId, ct);

            if (schedule is not null && schedule.Status != ScheduleStatus.OpenForPreferences)
                return BadRequest(new { error = "Preferences can only be deleted when schedule is OpenForPreferences" });
        }

        _db.EmployeePreferences.Remove(preference);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static PreferenceResponse MapToResponse(EmployeePreference preference) => new()
    {
        Id = preference.Id,
        EmployeeId = preference.EmployeeId,
        ShiftId = preference.ShiftId,
        Type = preference.Type,
        PeriodStart = preference.PeriodStart,
        PeriodEnd = preference.PeriodEnd,
        IsHard = preference.IsHard,
        CreatedAt = preference.CreatedAt
    };
}
