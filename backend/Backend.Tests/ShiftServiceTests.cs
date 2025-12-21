using Backend.Api.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class ShiftTests
{
    [Fact]
    public async Task Shift_CanBeCreatedForSchedule()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Week 1",
            WeekStartDate = new DateTime(2024, 12, 23),
            Status = ScheduleStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ShiftSchedules.Add(schedule);
        await context.SaveChangesAsync();

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
        context.Shifts.Add(shift);
        await context.SaveChangesAsync();

        // Assert
        var created = await context.Shifts.FindAsync(shift.Id);
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.ShiftScheduleId.Should().Be(schedule.Id);
        created.RequiredAbilities.Should().Contain("bartender");
    }

    [Fact]
    public async Task GetByScheduleId_ReturnsOnlyShiftsForThatSchedule()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var schedule1 = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var schedule2 = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 2", WeekStartDate = DateTime.Now.AddDays(7), Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.ShiftSchedules.AddRange(schedule1, schedule2);
        await context.SaveChangesAsync();

        context.Shifts.Add(new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule1.Id, Name = "Shift 1A", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        context.Shifts.Add(new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule1.Id, Name = "Shift 1B", StartTime = DateTime.Now.AddHours(6), EndTime = DateTime.Now.AddHours(12), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        context.Shifts.Add(new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule2.Id, Name = "Shift 2A", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act
        var schedule1Shifts = await context.Shifts.Where(s => s.ShiftScheduleId == schedule1.Id).ToListAsync();
        var schedule2Shifts = await context.Shifts.Where(s => s.ShiftScheduleId == schedule2.Id).ToListAsync();

        // Assert
        schedule1Shifts.Should().HaveCount(2);
        schedule2Shifts.Should().HaveCount(1);
    }

    [Fact]
    public async Task Delete_RemovesShift()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var schedule = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.ShiftSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var shift = new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule.Id, Name = "Morning", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.Shifts.Add(shift);
        await context.SaveChangesAsync();

        // Act
        context.Shifts.Remove(shift);
        await context.SaveChangesAsync();
        var retrieved = await context.Shifts.FindAsync(shift.Id);

        // Assert
        retrieved.Should().BeNull();
    }
}
