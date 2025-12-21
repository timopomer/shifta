using Backend.Api.Clients.Generated;
using Backend.Api.Data;
using Backend.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class ScheduleService
{
    private readonly AppDbContext _db;
    private readonly IOptimizerApiClient _optimizerClient;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(AppDbContext db, IOptimizerApiClient optimizerClient, ILogger<ScheduleService> logger)
    {
        _db = db;
        _optimizerClient = optimizerClient;
        _logger = logger;
    }

    public async Task<List<ShiftSchedule>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.ShiftSchedules
            .AsNoTracking()
            .OrderByDescending(s => s.WeekStartDate)
            .ToListAsync(ct);
    }

    public async Task<List<ShiftSchedule>> GetByStatusAsync(ScheduleStatus status, CancellationToken ct = default)
    {
        return await _db.ShiftSchedules
            .AsNoTracking()
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.WeekStartDate)
            .ToListAsync(ct);
    }

    public async Task<ShiftSchedule?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.ShiftSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<Shift>> GetShiftsAsync(Guid scheduleId, CancellationToken ct = default)
    {
        return await _db.Shifts
            .AsNoTracking()
            .Where(s => s.ShiftScheduleId == scheduleId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<List<ShiftAssignment>> GetAssignmentsAsync(Guid scheduleId, CancellationToken ct = default)
    {
        return await (
            from assignment in _db.ShiftAssignments
            join shift in _db.Shifts on assignment.ShiftId equals shift.Id
            where shift.ShiftScheduleId == scheduleId
            select assignment
        ).AsNoTracking().ToListAsync(ct);
    }

    public async Task<ShiftSchedule> CreateAsync(ShiftSchedule schedule, CancellationToken ct = default)
    {
        schedule.Id = Guid.NewGuid();
        schedule.Status = ScheduleStatus.Draft;
        schedule.CreatedAt = DateTime.UtcNow;
        schedule.UpdatedAt = DateTime.UtcNow;

        _db.ShiftSchedules.Add(schedule);
        await _db.SaveChangesAsync(ct);

        return schedule;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.ShiftSchedules.FindAsync([id], ct);
        if (entity is not null)
        {
            _db.ShiftSchedules.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }

    // Lifecycle methods

    public async Task<ShiftSchedule> OpenForPreferencesAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.ShiftSchedules.FindAsync([scheduleId], ct)
            ?? throw new InvalidOperationException($"Schedule {scheduleId} not found");

        if (schedule.Status != ScheduleStatus.Draft)
            throw new InvalidOperationException($"Cannot open schedule for preferences. Current status: {schedule.Status}. Expected: Draft");

        var shiftCount = await _db.Shifts.CountAsync(s => s.ShiftScheduleId == scheduleId, ct);
        if (shiftCount == 0)
            throw new InvalidOperationException("Cannot open schedule for preferences. No shifts have been added.");

        schedule.Status = ScheduleStatus.OpenForPreferences;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return schedule;
    }

    public async Task<ShiftSchedule> ClosePreferencesAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.ShiftSchedules.FindAsync([scheduleId], ct)
            ?? throw new InvalidOperationException($"Schedule {scheduleId} not found");

        if (schedule.Status != ScheduleStatus.OpenForPreferences)
            throw new InvalidOperationException($"Cannot close preferences. Current status: {schedule.Status}. Expected: OpenForPreferences");

        schedule.Status = ScheduleStatus.PendingReview;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return schedule;
    }

    public async Task<ShiftSchedule> FinalizeAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.ShiftSchedules.FindAsync([scheduleId], ct)
            ?? throw new InvalidOperationException($"Schedule {scheduleId} not found");

        if (schedule.Status != ScheduleStatus.PendingReview)
            throw new InvalidOperationException($"Cannot finalize schedule. Current status: {schedule.Status}. Expected: PendingReview");

        // Get all data needed for optimization
        var shifts = await _db.Shifts
            .Where(s => s.ShiftScheduleId == scheduleId)
            .ToListAsync(ct);

        var preferences = await (
            from p in _db.EmployeePreferences
            join s in _db.Shifts on p.ShiftId equals s.Id
            where s.ShiftScheduleId == scheduleId
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
            scheduleId, employees.Count, shifts.Count);

        // Call optimizer
        var response = await _optimizerClient.PostAsync(request, ct);

        if (!response.Success || response.Solutions == null || response.Solutions.Count == 0)
        {
            var error = response.Error?.AdditionalProperties.Values.FirstOrDefault()?.ToString() ?? "No solutions found";
            _logger.LogWarning("Optimization failed for schedule {ScheduleId}: {Error}", scheduleId, error);
            throw new InvalidOperationException($"Optimization failed: {error}");
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
            assignments.Count, scheduleId);

        // Update schedule status
        schedule.Status = ScheduleStatus.Finalized;
        schedule.FinalizedAt = now;
        schedule.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return schedule;
    }

    public async Task<ShiftSchedule> ArchiveAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.ShiftSchedules.FindAsync([scheduleId], ct)
            ?? throw new InvalidOperationException($"Schedule {scheduleId} not found");

        if (schedule.Status != ScheduleStatus.Finalized)
            throw new InvalidOperationException($"Cannot archive schedule. Current status: {schedule.Status}. Expected: Finalized");

        schedule.Status = ScheduleStatus.Archived;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return schedule;
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
        // The generated Preferences class uses AdditionalProperties for dynamic fields
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
}
