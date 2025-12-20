using Backend.Api.Dtos;
using Backend.Api.Entities;
using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly ScheduleService _scheduleService;

    public SchedulesController(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ScheduleResponse>>> GetAll(CancellationToken ct)
    {
        var schedules = await _scheduleService.GetAllAsync(ct);
        return schedules.Select(MapToResponse).ToList();
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<ScheduleResponse>>> GetByStatus(ScheduleStatus status, CancellationToken ct)
    {
        var schedules = await _scheduleService.GetByStatusAsync(status, ct);
        return schedules.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScheduleDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var schedule = await _scheduleService.GetByIdAsync(id, ct);
        if (schedule is null)
            return NotFound();

        var shifts = await _scheduleService.GetShiftsAsync(id, ct);
        var assignments = await _scheduleService.GetAssignmentsAsync(id, ct);

        return MapToDetailResponse(schedule, shifts, assignments);
    }

    [HttpPost]
    public async Task<ActionResult<ScheduleResponse>> Create(CreateScheduleRequest request, CancellationToken ct)
    {
        var schedule = new ShiftSchedule
        {
            Name = request.Name,
            WeekStartDate = request.WeekStartDate
        };

        var created = await _scheduleService.CreateAsync(schedule, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToResponse(created));
    }

    [HttpPost("{id:guid}/open")]
    public async Task<ActionResult<ScheduleResponse>> Open(Guid id, CancellationToken ct)
    {
        try
        {
            var schedule = await _scheduleService.OpenForPreferencesAsync(id, ct);
            return MapToResponse(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ScheduleResponse>> Close(Guid id, CancellationToken ct)
    {
        try
        {
            var schedule = await _scheduleService.ClosePreferencesAsync(id, ct);
            return MapToResponse(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/finalize")]
    public async Task<ActionResult<ScheduleResponse>> Finalize(Guid id, CancellationToken ct)
    {
        try
        {
            var schedule = await _scheduleService.FinalizeAsync(id, ct);
            return MapToResponse(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<ScheduleResponse>> Archive(Guid id, CancellationToken ct)
    {
        try
        {
            var schedule = await _scheduleService.ArchiveAsync(id, ct);
            return MapToResponse(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var schedule = await _scheduleService.GetByIdAsync(id, ct);
        if (schedule is null)
            return NotFound();

        if (schedule.Status != ScheduleStatus.Draft)
            return BadRequest(new { error = "Can only delete schedules in Draft status" });

        await _scheduleService.DeleteAsync(id, ct);
        return NoContent();
    }

    private static ScheduleResponse MapToResponse(ShiftSchedule schedule) => new()
    {
        Id = schedule.Id,
        Name = schedule.Name,
        WeekStartDate = schedule.WeekStartDate,
        Status = schedule.Status,
        CreatedAt = schedule.CreatedAt,
        UpdatedAt = schedule.UpdatedAt,
        FinalizedAt = schedule.FinalizedAt
    };

    private static ScheduleDetailResponse MapToDetailResponse(
        ShiftSchedule schedule,
        List<Shift> shifts,
        List<ShiftAssignment> assignments) => new()
    {
        Id = schedule.Id,
        Name = schedule.Name,
        WeekStartDate = schedule.WeekStartDate,
        Status = schedule.Status,
        CreatedAt = schedule.CreatedAt,
        UpdatedAt = schedule.UpdatedAt,
        FinalizedAt = schedule.FinalizedAt,
        Shifts = shifts.Select(s => new ShiftResponse
        {
            Id = s.Id,
            ShiftScheduleId = s.ShiftScheduleId,
            Name = s.Name,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            RequiredAbilities = s.RequiredAbilities,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList(),
        Assignments = assignments.Select(a => new ShiftAssignmentResponse
        {
            Id = a.Id,
            ShiftId = a.ShiftId,
            EmployeeId = a.EmployeeId,
            AssignedAt = a.AssignedAt
        }).ToList()
    };
}
