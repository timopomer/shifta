using Backend.Api.Dtos;
using Backend.Api.Entities;
using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PreferencesController : ControllerBase
{
    private readonly PreferenceService _preferenceService;
    private readonly ShiftService _shiftService;
    private readonly ScheduleService _scheduleService;
    private readonly EmployeeService _employeeService;

    public PreferencesController(
        PreferenceService preferenceService,
        ShiftService shiftService,
        ScheduleService scheduleService,
        EmployeeService employeeService)
    {
        _preferenceService = preferenceService;
        _shiftService = shiftService;
        _scheduleService = scheduleService;
        _employeeService = employeeService;
    }

    [HttpGet("schedule/{scheduleId:guid}")]
    public async Task<ActionResult<List<PreferenceResponse>>> GetBySchedule(Guid scheduleId, CancellationToken ct)
    {
        var preferences = await _preferenceService.GetByScheduleIdAsync(scheduleId, ct);
        return preferences.Select(MapToResponse).ToList();
    }

    [HttpGet("employee/{employeeId:guid}")]
    public async Task<ActionResult<List<PreferenceResponse>>> GetByEmployee(Guid employeeId, CancellationToken ct)
    {
        var preferences = await _preferenceService.GetByEmployeeIdAsync(employeeId, ct);
        return preferences.Select(MapToResponse).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PreferenceResponse>> GetById(Guid id, CancellationToken ct)
    {
        var preference = await _preferenceService.GetByIdAsync(id, ct);
        if (preference is null)
            return NotFound();

        return MapToResponse(preference);
    }

    [HttpPost]
    public async Task<ActionResult<PreferenceResponse>> Create(CreatePreferenceRequest request, CancellationToken ct)
    {
        // Validate the shift exists
        var shift = await _shiftService.GetByIdAsync(request.ShiftId, ct);
        if (shift is null)
            return BadRequest(new { error = "Shift not found" });

        // Get the schedule to check status
        var schedule = await _scheduleService.GetByIdAsync(shift.ShiftScheduleId, ct);
        if (schedule is null)
            return BadRequest(new { error = "Schedule not found" });

        if (schedule.Status != ScheduleStatus.OpenForPreferences)
            return BadRequest(new { error = "Preferences can only be submitted when schedule is OpenForPreferences" });

        // Validate the employee exists
        var employee = await _employeeService.GetByIdAsync(request.EmployeeId, ct);
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
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            Type = request.Type,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            IsHard = request.IsHard
        };

        var created = await _preferenceService.CreateAsync(preference, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToResponse(created));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var preference = await _preferenceService.GetByIdAsync(id, ct);
        if (preference is null)
            return NotFound();

        // Get the shift and schedule to check status
        var shift = await _shiftService.GetByIdAsync(preference.ShiftId, ct);
        if (shift is not null)
        {
            var schedule = await _scheduleService.GetByIdAsync(shift.ShiftScheduleId, ct);
            if (schedule is not null && schedule.Status != ScheduleStatus.OpenForPreferences)
                return BadRequest(new { error = "Preferences can only be deleted when schedule is OpenForPreferences" });
        }

        await _preferenceService.DeleteAsync(id, ct);
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
