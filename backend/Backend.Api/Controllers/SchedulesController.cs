using Backend.Api.Clients.Generated;
using Backend.Api.Data;
using Backend.Api.Dtos;
using Backend.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOptimizerApiClient _optimizerClient;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(AppDbContext db, IOptimizerApiClient optimizerClient, ILogger<SchedulesController> logger)
    {
        _db = db;
        _optimizerClient = optimizerClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ScheduleResponse>>> GetAll(CancellationToken ct)
    {
        var schedules = await _db.ShiftSchedules
            .AsNoTracking()
            .OrderByDescending(s => s.WeekStartDate)
            .ToListAsync(ct);

        return schedules.Select(MapToResponse).ToList();
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<ScheduleResponse>>> GetByStatus(ScheduleStatus status, CancellationToken ct)
    {
        var schedules = await _db.ShiftSchedules
            .AsNoTracking()
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.WeekStartDate)
            .ToListAsync(ct);

        return schedules.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScheduleDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var schedule = await _db.ShiftSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (schedule is null)
            return NotFound();

        var shifts = await _db.Shifts
            .AsNoTracking()
            .Where(s => s.ShiftScheduleId == id)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

        var assignments = await (
            from assignment in _db.ShiftAssignments
            join shift in _db.Shifts on assignment.ShiftId equals shift.Id
            where shift.ShiftScheduleId == id
            select assignment
        ).AsNoTracking().ToListAsync(ct);

        return MapToDetailResponse(schedule, shifts, assignments);
    }

    [HttpPost]
    public async Task<ActionResult<ScheduleResponse>> Create(CreateScheduleRequest request, CancellationToken ct)
    {
        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            WeekStartDate = request.WeekStartDate,
            Status = ScheduleStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ShiftSchedules.Add(schedule);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, MapToResponse(schedule));
    }

    [HttpPost("{id:guid}/open")]
    public async Task<ActionResult<ScheduleResponse>> Open(Guid id, CancellationToken ct)
    {
        var schedule = await _db.ShiftSchedules.FindAsync([id], ct);
        if (schedule is null)
            return NotFound();

        if (schedule.Status != ScheduleStatus.Draft)
            return BadRequest(new { error = $"Cannot open schedule for preferences. Current status: {schedule.Status}. Expected: Draft" });

        var shiftCount = await _db.Shifts.CountAsync(s => s.ShiftScheduleId == id, ct);
        if (shiftCount == 0)
            return BadRequest(new { error = "Cannot open schedule for preferences. No shifts have been added." });

        schedule.Status = ScheduleStatus.OpenForPreferences;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return MapToResponse(schedule);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ScheduleResponse>> Close(Guid id, CancellationToken ct)
    {
        var schedule = await _db.ShiftSchedules.FindAsync([id], ct);
        if (schedule is null)
            return NotFound();

        if (schedule.Status != ScheduleStatus.OpenForPreferences)
            return BadRequest(new { error = $"Cannot close preferences. Current status: {schedule.Status}. Expected: OpenForPreferences" });

        schedule.Status = ScheduleStatus.PendingReview;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return MapToResponse(schedule);
    }

    [HttpPost("{id:guid}/finalize")]
    public async Task<ActionResult<ScheduleResponse>> Finalize(Guid id, CancellationToken ct)
    {
        var schedule = await _db.ShiftSchedules.FindAsync([id], ct);
        if (schedule is null)
            return NotFound();

        if (schedule.Status != ScheduleStatus.PendingReview)
            return BadRequest(new { error = $"Cannot finalize schedule. Current status: {schedule.Status}. Expected: PendingReview" });

        // Get all data needed for optimization
        var shifts = await _db.Shifts
            .Where(s => s.ShiftScheduleId == id)
            .ToListAsync(ct);

        var preferences = await (
            from p in _db.EmployeePreferences
            join s in _db.Shifts on p.ShiftId equals s.Id
            where s.ShiftScheduleId == id
            select p
        ).ToListAsync(ct);

        var employees = await _db.Employees.ToListAsync(ct);

        // Group preferences by employee
        var employeePreferences = preferences
            .GroupBy(p => p.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build optimization request
        var request = BuildOptimizeRequest(employees, shifts, employeePreferences);

        _logger.LogInformation("Sending optimization request for schedule {ScheduleId} with {EmployeeCount} employees and {ShiftCount} shifts",
            id, employees.Count, shifts.Count);

        // Call optimizer
        var response = await _optimizerClient.PostAsync(request, ct);

        if (!response.Success || response.Solutions == null || response.Solutions.Count == 0)
        {
            var error = response.Error?.AdditionalProperties.Values.FirstOrDefault()?.ToString() ?? "No solutions found";
            _logger.LogWarning("Optimization failed for schedule {ScheduleId}: {Error}", id, error);
            return BadRequest(new { error = $"Optimization failed: {error}" });
        }

        // Take the best solution
        var solution = response.Solutions[0];

        // Clear any existing assignments for this schedule
        var shiftIds = shifts.Select(s => s.Id).ToList();
        var existingAssignments = await _db.ShiftAssignments
            .Where(a => shiftIds.Contains(a.ShiftId))
            .ToListAsync(ct);
        _db.ShiftAssignments.RemoveRange(existingAssignments);

        // Create new assignments
        var now = DateTime.UtcNow;
        var assignments = solution.Assignments.Select(kv => new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = Guid.Parse(kv.Key),
            EmployeeId = Guid.Parse(kv.Value),
            AssignedAt = now
        }).ToList();

        _db.ShiftAssignments.AddRange(assignments);

        _logger.LogInformation("Created {AssignmentCount} assignments for schedule {ScheduleId}",
            assignments.Count, id);

        // Update schedule status
        schedule.Status = ScheduleStatus.Finalized;
        schedule.FinalizedAt = now;
        schedule.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return MapToResponse(schedule);
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<ScheduleResponse>> Archive(Guid id, CancellationToken ct)
    {
        var schedule = await _db.ShiftSchedules.FindAsync([id], ct);
        if (schedule is null)
            return NotFound();

        if (schedule.Status != ScheduleStatus.Finalized)
            return BadRequest(new { error = $"Cannot archive schedule. Current status: {schedule.Status}. Expected: Finalized" });

        schedule.Status = ScheduleStatus.Archived;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return MapToResponse(schedule);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var schedule = await _db.ShiftSchedules.FindAsync([id], ct);
        if (schedule is null)
            return NotFound();

        if (schedule.Status != ScheduleStatus.Draft)
            return BadRequest(new { error = "Can only delete schedules in Draft status" });

        _db.ShiftSchedules.Remove(schedule);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static OptimizeRequest BuildOptimizeRequest(
        List<Employee> employees,
        List<Shift> shifts,
        Dictionary<Guid, List<EmployeePreference>> employeePreferences)
    {
        var employeeDtos = employees.Select(e => new EmployeeDto
        {
            Id = e.Id.ToString(),
            Name = e.Name,
            Abilities = e.Abilities,
            Preferences = employeePreferences.TryGetValue(e.Id, out var prefs)
                ? prefs.Select(MapPreference).ToList()
                : []
        }).ToList();

        var shiftDtos = shifts.Select(s => new ShiftDto
        {
            Id = s.Id.ToString(),
            Name = s.Name,
            Start_time = s.StartTime,
            End_time = s.EndTime,
            Required_abilities = s.RequiredAbilities
        }).ToList();

        return new OptimizeRequest
        {
            Employees = employeeDtos,
            Shifts = shiftDtos,
            Max_solutions = 1
        };
    }

    private static Preferences MapPreference(EmployeePreference pref)
    {
        var preference = new Preferences();

        switch (pref.Type)
        {
            case PreferenceType.PreferShift:
                preference.AdditionalProperties["type"] = "prefer_shift";
                preference.AdditionalProperties["shift_id"] = pref.ShiftId.ToString();
                preference.AdditionalProperties["is_hard"] = pref.IsHard;
                break;
            case PreferenceType.PreferPeriod:
                preference.AdditionalProperties["type"] = "prefer_period";
                preference.AdditionalProperties["start"] = pref.PeriodStart?.ToString("o");
                preference.AdditionalProperties["end"] = pref.PeriodEnd?.ToString("o");
                preference.AdditionalProperties["is_hard"] = pref.IsHard;
                break;
            case PreferenceType.Unavailable:
                preference.AdditionalProperties["type"] = "unavailable_period";
                preference.AdditionalProperties["start"] = pref.PeriodStart?.ToString("o");
                preference.AdditionalProperties["end"] = pref.PeriodEnd?.ToString("o");
                preference.AdditionalProperties["is_hard"] = pref.IsHard;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pref), $"Unknown preference type: {pref.Type}");
        }

        return preference;
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
