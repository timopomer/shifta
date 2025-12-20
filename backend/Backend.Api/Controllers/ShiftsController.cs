using Backend.Api.Dtos;
using Backend.Api.Entities;
using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/schedules/{scheduleId:guid}/shifts")]
public class ShiftsController : ControllerBase
{
    private readonly ShiftService _shiftService;
    private readonly ScheduleService _scheduleService;

    public ShiftsController(ShiftService shiftService, ScheduleService scheduleService)
    {
        _shiftService = shiftService;
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ShiftResponse>>> GetAll(Guid scheduleId, CancellationToken ct)
    {
        var shifts = await _shiftService.GetByScheduleIdAsync(scheduleId, ct);
        return shifts.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShiftResponse>> GetById(Guid scheduleId, Guid id, CancellationToken ct)
    {
        var shift = await _shiftService.GetByIdAsync(id, ct);
        if (shift is null || shift.ShiftScheduleId != scheduleId)
            return NotFound();

        return MapToResponse(shift);
    }

    [HttpPost]
    public async Task<ActionResult<ShiftResponse>> Create(Guid scheduleId, CreateShiftRequest request, CancellationToken ct)
    {
        var schedule = await _scheduleService.GetByIdAsync(scheduleId, ct);
        if (schedule is null)
            return NotFound(new { error = "Schedule not found" });

        if (schedule.Status != ScheduleStatus.Draft)
            return BadRequest(new { error = "Can only add shifts to schedules in Draft status" });

        if (request.EndTime <= request.StartTime)
            return BadRequest(new { error = "EndTime must be after StartTime" });

        var shift = new Shift
        {
            ShiftScheduleId = scheduleId,
            Name = request.Name,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            RequiredAbilities = request.RequiredAbilities
        };

        var created = await _shiftService.CreateAsync(shift, ct);
        return CreatedAtAction(nameof(GetById), new { scheduleId, id = created.Id }, MapToResponse(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ShiftResponse>> Update(Guid scheduleId, Guid id, UpdateShiftRequest request, CancellationToken ct)
    {
        var schedule = await _scheduleService.GetByIdAsync(scheduleId, ct);
        if (schedule is null)
            return NotFound(new { error = "Schedule not found" });

        if (schedule.Status != ScheduleStatus.Draft)
            return BadRequest(new { error = "Can only update shifts in schedules with Draft status" });

        var existing = await _shiftService.GetByIdAsync(id, ct);
        if (existing is null || existing.ShiftScheduleId != scheduleId)
            return NotFound();

        if (request.EndTime <= request.StartTime)
            return BadRequest(new { error = "EndTime must be after StartTime" });

        existing.Name = request.Name;
        existing.StartTime = request.StartTime;
        existing.EndTime = request.EndTime;
        existing.RequiredAbilities = request.RequiredAbilities;

        var updated = await _shiftService.UpdateAsync(existing, ct);
        return MapToResponse(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid scheduleId, Guid id, CancellationToken ct)
    {
        var schedule = await _scheduleService.GetByIdAsync(scheduleId, ct);
        if (schedule is null)
            return NotFound(new { error = "Schedule not found" });

        if (schedule.Status != ScheduleStatus.Draft)
            return BadRequest(new { error = "Can only delete shifts from schedules in Draft status" });

        var existing = await _shiftService.GetByIdAsync(id, ct);
        if (existing is null || existing.ShiftScheduleId != scheduleId)
            return NotFound();

        await _shiftService.DeleteAsync(id, ct);
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
