using Backend.Api.Data;
using Backend.Api.Dtos;
using Backend.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/time-off-requests")]
public class TimeOffRequestsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TimeOffRequestsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TimeOffRequestResponse>>> GetAll(CancellationToken ct)
    {
        return await GetTimeOffRequestsQuery().ToListAsync(ct);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TimeOffRequestResponse>> GetById(Guid id, CancellationToken ct)
    {
        var request = await GetTimeOffRequestsQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (request is null)
            return NotFound();

        return request;
    }

    [HttpGet("by-employee/{employeeId:guid}")]
    public async Task<ActionResult<List<TimeOffRequestResponse>>> GetByEmployee(Guid employeeId, CancellationToken ct)
    {
        return await GetTimeOffRequestsQuery()
            .Where(r => r.EmployeeId == employeeId)
            .ToListAsync(ct);
    }

    [HttpGet("by-manager/{managerId:guid}")]
    public async Task<ActionResult<List<TimeOffRequestResponse>>> GetByManager(Guid managerId, CancellationToken ct)
    {
        // Get all employees managed by this manager
        var managedEmployeeIds = await _db.ManagerEmployees
            .Where(me => me.ManagerId == managerId)
            .Select(me => me.EmployeeId)
            .ToListAsync(ct);

        return await GetTimeOffRequestsQuery()
            .Where(r => managedEmployeeIds.Contains(r.EmployeeId))
            .ToListAsync(ct);
    }

    [HttpGet("pending/by-manager/{managerId:guid}")]
    public async Task<ActionResult<List<TimeOffRequestResponse>>> GetPendingByManager(Guid managerId, CancellationToken ct)
    {
        // Get all employees managed by this manager
        var managedEmployeeIds = await _db.ManagerEmployees
            .Where(me => me.ManagerId == managerId)
            .Select(me => me.EmployeeId)
            .ToListAsync(ct);

        return await GetTimeOffRequestsQuery()
            .Where(r => managedEmployeeIds.Contains(r.EmployeeId) && r.Status == RequestStatus.Pending)
            .ToListAsync(ct);
    }

    [HttpPost]
    public async Task<ActionResult<TimeOffRequestResponse>> Create(CreateTimeOffRequestRequest request, CancellationToken ct)
    {
        // Validate employee exists
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);

        if (employee is null)
            return BadRequest(new { error = "Employee not found" });

        // Validate dates
        if (request.EndDate < request.StartDate)
            return BadRequest(new { error = "End date must be after start date" });

        // Check for overlapping requests
        var hasOverlap = await _db.TimeOffRequests
            .AnyAsync(r => r.EmployeeId == request.EmployeeId
                && r.Status != RequestStatus.Rejected
                && r.StartDate < request.EndDate
                && r.EndDate > request.StartDate, ct);

        if (hasOverlap)
            return BadRequest(new { error = "This time off request overlaps with an existing request" });

        var now = DateTime.UtcNow;
        var timeOffRequest = new TimeOffRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = RequestStatus.Pending,
            Reason = request.Reason,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.TimeOffRequests.Add(timeOffRequest);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = timeOffRequest.Id },
            await GetTimeOffRequestsQuery().FirstAsync(r => r.Id == timeOffRequest.Id, ct));
    }

    [HttpPut("{id:guid}/review")]
    public async Task<ActionResult<TimeOffRequestResponse>> Review(Guid id, [FromBody] ReviewTimeOffRequestRequest request, [FromQuery] Guid reviewerId, CancellationToken ct)
    {
        var timeOffRequest = await _db.TimeOffRequests.FindAsync([id], ct);
        if (timeOffRequest is null)
            return NotFound();

        if (timeOffRequest.Status != RequestStatus.Pending)
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

        timeOffRequest.Status = request.Status;
        timeOffRequest.ReviewedById = reviewerId;
        timeOffRequest.ReviewedAt = DateTime.UtcNow;
        timeOffRequest.ReviewNote = request.ReviewNote;
        timeOffRequest.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetTimeOffRequestsQuery().FirstAsync(r => r.Id == id, ct);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var request = await _db.TimeOffRequests.FindAsync([id], ct);
        if (request is null)
            return NotFound();

        if (request.Status != RequestStatus.Pending)
            return BadRequest(new { error = "Cannot delete a request that has been reviewed" });

        _db.TimeOffRequests.Remove(request);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private IQueryable<TimeOffRequestResponse> GetTimeOffRequestsQuery()
    {
        return from tor in _db.TimeOffRequests
               join employee in _db.Employees on tor.EmployeeId equals employee.Id
               join reviewer in _db.Employees on tor.ReviewedById equals reviewer.Id into reviewerJoin
               from reviewer in reviewerJoin.DefaultIfEmpty()
               orderby tor.CreatedAt descending
               select new TimeOffRequestResponse
               {
                   Id = tor.Id,
                   EmployeeId = tor.EmployeeId,
                   EmployeeName = employee.Name,
                   StartDate = tor.StartDate,
                   EndDate = tor.EndDate,
                   Status = tor.Status,
                   Reason = tor.Reason,
                   CreatedAt = tor.CreatedAt,
                   UpdatedAt = tor.UpdatedAt,
                   ReviewedById = tor.ReviewedById,
                   ReviewedByName = reviewer != null ? reviewer.Name : null,
                   ReviewedAt = tor.ReviewedAt,
                   ReviewNote = tor.ReviewNote
               };
    }
}


