using Backend.Api.Data;
using Backend.Api.Dtos;
using Backend.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/shift-requests")]
public class ShiftRequestsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ShiftRequestsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ShiftRequestResponse>>> GetAll(CancellationToken ct)
    {
        return await GetShiftRequestsQuery().ToListAsync(ct);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShiftRequestResponse>> GetById(Guid id, CancellationToken ct)
    {
        var request = await GetShiftRequestsQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (request is null)
            return NotFound();

        return request;
    }

    [HttpGet("by-employee/{employeeId:guid}")]
    public async Task<ActionResult<List<ShiftRequestResponse>>> GetByEmployee(Guid employeeId, CancellationToken ct)
    {
        return await GetShiftRequestsQuery()
            .Where(r => r.EmployeeId == employeeId)
            .ToListAsync(ct);
    }

    [HttpGet("by-schedule/{scheduleId:guid}")]
    public async Task<ActionResult<List<ShiftRequestResponse>>> GetBySchedule(Guid scheduleId, CancellationToken ct)
    {
        return await GetShiftRequestsQuery()
            .Where(r => r.ScheduleId == scheduleId)
            .ToListAsync(ct);
    }

    [HttpGet("by-manager/{managerId:guid}")]
    public async Task<ActionResult<List<ShiftRequestResponse>>> GetByManager(Guid managerId, CancellationToken ct)
    {
        // Get all employees managed by this manager
        var managedEmployeeIds = await _db.ManagerEmployees
            .Where(me => me.ManagerId == managerId)
            .Select(me => me.EmployeeId)
            .ToListAsync(ct);

        return await GetShiftRequestsQuery()
            .Where(r => managedEmployeeIds.Contains(r.EmployeeId))
            .ToListAsync(ct);
    }

    [HttpGet("pending/by-manager/{managerId:guid}")]
    public async Task<ActionResult<List<ShiftRequestResponse>>> GetPendingByManager(Guid managerId, CancellationToken ct)
    {
        // Get all employees managed by this manager
        var managedEmployeeIds = await _db.ManagerEmployees
            .Where(me => me.ManagerId == managerId)
            .Select(me => me.EmployeeId)
            .ToListAsync(ct);

        return await GetShiftRequestsQuery()
            .Where(r => managedEmployeeIds.Contains(r.EmployeeId) && r.Status == RequestStatus.Pending)
            .ToListAsync(ct);
    }

    [HttpPost]
    public async Task<ActionResult<ShiftRequestResponse>> Create(CreateShiftRequestRequest request, CancellationToken ct)
    {
        // Validate employee exists
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);

        if (employee is null)
            return BadRequest(new { error = "Employee not found" });

        // Validate shift exists
        var shift = await _db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct);

        if (shift is null)
            return BadRequest(new { error = "Shift not found" });

        // Check for duplicate
        var exists = await _db.ShiftRequests
            .AnyAsync(r => r.EmployeeId == request.EmployeeId && r.ShiftId == request.ShiftId, ct);

        if (exists)
            return BadRequest(new { error = "A request for this shift already exists" });

        var now = DateTime.UtcNow;
        var shiftRequest = new ShiftRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            RequestType = request.RequestType,
            Status = RequestStatus.Pending,
            Note = request.Note,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ShiftRequests.Add(shiftRequest);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = shiftRequest.Id },
            await GetShiftRequestsQuery().FirstAsync(r => r.Id == shiftRequest.Id, ct));
    }

    [HttpPut("{id:guid}/review")]
    public async Task<ActionResult<ShiftRequestResponse>> Review(Guid id, [FromBody] ReviewShiftRequestRequest request, [FromQuery] Guid reviewerId, CancellationToken ct)
    {
        var shiftRequest = await _db.ShiftRequests.FindAsync([id], ct);
        if (shiftRequest is null)
            return NotFound();

        if (shiftRequest.Status != RequestStatus.Pending)
            return BadRequest(new { error = "This request has already been reviewed" });

        // Validate reviewer exists and is a manager
        var reviewer = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == reviewerId, ct);

        if (reviewer is null || !reviewer.IsManager)
            return BadRequest(new { error = "Invalid reviewer" });

        // Validate status is not Pending (must approve or reject)
        if (request.Status == RequestStatus.Pending)
            return BadRequest(new { error = "Status must be Approved or Rejected" });

        shiftRequest.Status = request.Status;
        shiftRequest.ReviewedById = reviewerId;
        shiftRequest.ReviewedAt = DateTime.UtcNow;
        shiftRequest.ReviewNote = request.ReviewNote;
        shiftRequest.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetShiftRequestsQuery().FirstAsync(r => r.Id == id, ct);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var request = await _db.ShiftRequests.FindAsync([id], ct);
        if (request is null)
            return NotFound();

        if (request.Status != RequestStatus.Pending)
            return BadRequest(new { error = "Cannot delete a request that has been reviewed" });

        _db.ShiftRequests.Remove(request);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private IQueryable<ShiftRequestResponse> GetShiftRequestsQuery()
    {
        return from sr in _db.ShiftRequests
               join employee in _db.Employees on sr.EmployeeId equals employee.Id
               join shift in _db.Shifts on sr.ShiftId equals shift.Id
               join schedule in _db.ShiftSchedules on shift.ShiftScheduleId equals schedule.Id
               join reviewer in _db.Employees on sr.ReviewedById equals reviewer.Id into reviewerJoin
               from reviewer in reviewerJoin.DefaultIfEmpty()
               orderby sr.CreatedAt descending
               select new ShiftRequestResponse
               {
                   Id = sr.Id,
                   EmployeeId = sr.EmployeeId,
                   EmployeeName = employee.Name,
                   ShiftId = sr.ShiftId,
                   ShiftName = shift.Name,
                   ShiftStartTime = shift.StartTime,
                   ShiftEndTime = shift.EndTime,
                   ScheduleId = schedule.Id,
                   ScheduleName = schedule.Name,
                   RequestType = sr.RequestType,
                   Status = sr.Status,
                   Note = sr.Note,
                   CreatedAt = sr.CreatedAt,
                   UpdatedAt = sr.UpdatedAt,
                   ReviewedById = sr.ReviewedById,
                   ReviewedByName = reviewer != null ? reviewer.Name : null,
                   ReviewedAt = sr.ReviewedAt,
                   ReviewNote = sr.ReviewNote
               };
    }
}


