using Backend.Api.Data;
using Backend.Api.Dtos;
using Backend.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/schedules/{scheduleId:guid}/shifts")]
public class ShiftsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ShiftsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ShiftResponse>>> GetAll(Guid scheduleId, CancellationToken ct)
    {
        var shifts = await _db.Shifts
            .AsNoTracking()
            .Where(s => s.ShiftScheduleId == scheduleId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

        return shifts.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShiftResponse>> GetById(Guid scheduleId, Guid id, CancellationToken ct)
    {
        var shift = await _db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (shift is null || shift.ShiftScheduleId != scheduleId)
            return NotFound();

        return MapToResponse(shift);
    }

    [HttpPost]
    public async Task<ActionResult<ShiftResponse>> Create(Guid scheduleId, CreateShiftRequest request, CancellationToken ct)
    {
        var schedule = await _db.ShiftSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct);

        if (schedule is null)
            return NotFound(new { error = "Schedule not found" });

        if (schedule.Status != ScheduleStatus.Draft)
            return BadRequest(new { error = "Can only add shifts to schedules in Draft status" });

        if (request.EndTime <= request.StartTime)
            return BadRequest(new { error = "EndTime must be after StartTime" });

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            ShiftScheduleId = scheduleId,
            Name = request.Name,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RequiredAbilities = request.RequiredAbilities,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { scheduleId, id = shift.Id }, MapToResponse(shift));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ShiftResponse>> Update(Guid scheduleId, Guid id, UpdateShiftRequest request, CancellationToken ct)
    {
        var schedule = await _db.ShiftSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct);

        if (schedule is null)
            return NotFound(new { error = "Schedule not found" });

        if (schedule.Status != ScheduleStatus.Draft)
            return BadRequest(new { error = "Can only update shifts in schedules with Draft status" });

        var existing = await _db.Shifts.FindAsync([id], ct);
        if (existing is null || existing.ShiftScheduleId != scheduleId)
            return NotFound();

        if (request.EndTime <= request.StartTime)
            return BadRequest(new { error = "EndTime must be after StartTime" });

        existing.Name = request.Name;
        existing.StartTime = request.StartTime;
        existing.EndTime = request.EndTime;
        existing.RequiredAbilities = request.RequiredAbilities;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return MapToResponse(existing);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid scheduleId, Guid id, CancellationToken ct)
    {
        var schedule = await _db.ShiftSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct);

        if (schedule is null)
            return NotFound(new { error = "Schedule not found" });

        if (schedule.Status != ScheduleStatus.Draft)
            return BadRequest(new { error = "Can only delete shifts from schedules in Draft status" });

        var existing = await _db.Shifts.FindAsync([id], ct);
        if (existing is null || existing.ShiftScheduleId != scheduleId)
            return NotFound();

        _db.Shifts.Remove(existing);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static ShiftResponse MapToResponse(Shift shift) => new()
    {
        Id = shift.Id,
        ShiftScheduleId = shift.ShiftScheduleId,
        Name = shift.Name,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        RequiredAbilities = shift.RequiredAbilities,
        CreatedAt = shift.CreatedAt,
        UpdatedAt = shift.UpdatedAt
    };
}
