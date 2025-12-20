using Backend.Api.Entities;
using Backend.Api.Services;
using FluentAssertions;

namespace Backend.Tests;

public class ShiftServiceTests
{
    [Fact]
    public async Task Shift_CanBeCreatedForSchedule()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var shiftService = new ShiftService(context);

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
        var shift = await shiftService.CreateAsync(new Shift
        {
            ShiftScheduleId = schedule.Id,
            Name = "Morning",
            StartTime = new DateTime(2024, 12, 23, 8, 0, 0),
            EndTime = new DateTime(2024, 12, 23, 14, 0, 0),
            RequiredAbilities = ["bartender", "waiter"]
        });

        // Assert
        shift.Id.Should().NotBeEmpty();
        shift.ShiftScheduleId.Should().Be(schedule.Id);
        shift.RequiredAbilities.Should().Contain("bartender");
    }

    [Fact]
    public async Task GetByScheduleId_ReturnsOnlyShiftsForThatSchedule()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var shiftService = new ShiftService(context);

        var schedule1 = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var schedule2 = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 2", WeekStartDate = DateTime.Now.AddDays(7), Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.ShiftSchedules.AddRange(schedule1, schedule2);
        await context.SaveChangesAsync();

        await shiftService.CreateAsync(new Shift { ShiftScheduleId = schedule1.Id, Name = "Shift 1A", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6) });
        await shiftService.CreateAsync(new Shift { ShiftScheduleId = schedule1.Id, Name = "Shift 1B", StartTime = DateTime.Now.AddHours(6), EndTime = DateTime.Now.AddHours(12) });
        await shiftService.CreateAsync(new Shift { ShiftScheduleId = schedule2.Id, Name = "Shift 2A", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6) });

        // Act
        var schedule1Shifts = await shiftService.GetByScheduleIdAsync(schedule1.Id);
        var schedule2Shifts = await shiftService.GetByScheduleIdAsync(schedule2.Id);

        // Assert
        schedule1Shifts.Should().HaveCount(2);
        schedule2Shifts.Should().HaveCount(1);
    }

    [Fact]
    public async Task Delete_RemovesShift()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var shiftService = new ShiftService(context);

        var schedule = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        context.ShiftSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var shift = await shiftService.CreateAsync(new Shift { ShiftScheduleId = schedule.Id, Name = "Morning", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6) });

        // Act
        await shiftService.DeleteAsync(shift.Id);
        var retrieved = await shiftService.GetByIdAsync(shift.Id);

        // Assert
        retrieved.Should().BeNull();
    }
}
