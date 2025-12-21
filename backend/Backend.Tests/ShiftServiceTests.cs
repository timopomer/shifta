using Backend.Api.Data;
using Backend.Api.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

[Collection("Postgres")]
public class ShiftTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private AppDbContext _context = null!;

    public ShiftTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _context = _fixture.CreateDbContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        _context.Shifts.RemoveRange(_context.Shifts);
        _context.ShiftSchedules.RemoveRange(_context.ShiftSchedules);
        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Shift_CanBeCreatedForSchedule()
    {
        // Arrange
        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Week 1",
            WeekStartDate = new DateTime(2024, 12, 23),
            Status = ScheduleStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ShiftSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            ShiftScheduleId = schedule.Id,
            Name = "Morning",
            StartTime = new DateTime(2024, 12, 23, 8, 0, 0),
            EndTime = new DateTime(2024, 12, 23, 14, 0, 0),
            RequiredAbilities = ["bartender", "waiter"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        // Assert
        var created = await _context.Shifts.FindAsync(shift.Id);
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.ShiftScheduleId.Should().Be(schedule.Id);
        created.RequiredAbilities.Should().Contain("bartender");
    }

    [Fact]
    public async Task GetByScheduleId_ReturnsOnlyShiftsForThatSchedule()
    {
        // Arrange
        var schedule1 = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var schedule2 = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 2", WeekStartDate = DateTime.Now.AddDays(7), Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.ShiftSchedules.AddRange(schedule1, schedule2);
        await _context.SaveChangesAsync();

        _context.Shifts.Add(new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule1.Id, Name = "Shift 1A", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _context.Shifts.Add(new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule1.Id, Name = "Shift 1B", StartTime = DateTime.Now.AddHours(6), EndTime = DateTime.Now.AddHours(12), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _context.Shifts.Add(new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule2.Id, Name = "Shift 2A", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var schedule1Shifts = await _context.Shifts.Where(s => s.ShiftScheduleId == schedule1.Id).ToListAsync();
        var schedule2Shifts = await _context.Shifts.Where(s => s.ShiftScheduleId == schedule2.Id).ToListAsync();

        // Assert
        schedule1Shifts.Should().HaveCount(2);
        schedule2Shifts.Should().HaveCount(1);
    }

    [Fact]
    public async Task Delete_RemovesShift()
    {
        // Arrange
        var schedule = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.ShiftSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        var shift = new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule.Id, Name = "Morning", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();

        // Act
        _context.Shifts.Remove(shift);
        await _context.SaveChangesAsync();
        var retrieved = await _context.Shifts.FindAsync(shift.Id);

        // Assert
        retrieved.Should().BeNull();
    }
}
