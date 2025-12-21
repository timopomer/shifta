using Backend.Api.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class ScheduleLifecycleTests
{
    [Fact]
    public async Task Schedule_StartsInDraftStatus()
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

        // Act
        context.ShiftSchedules.Add(schedule);
        await context.SaveChangesAsync();

        // Assert
        var created = await context.ShiftSchedules.FindAsync(schedule.Id);
        created.Should().NotBeNull();
        created!.Status.Should().Be(ScheduleStatus.Draft);
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        created.FinalizedAt.Should().BeNull();
    }

    [Fact]
    public async Task Schedule_CanTransitionFromDraftToOpenForPreferences()
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

        // Add a shift (required to open for preferences)
        context.Shifts.Add(new Shift
        {
            Id = Guid.NewGuid(),
            ShiftScheduleId = schedule.Id,
            Name = "Morning",
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddHours(6),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        schedule.Status = ScheduleStatus.OpenForPreferences;
        schedule.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.ShiftSchedules.FindAsync(schedule.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(ScheduleStatus.OpenForPreferences);
    }

    [Fact]
    public async Task Schedule_CanTransitionFromOpenForPreferencesToPendingReview()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Week 1",
            WeekStartDate = new DateTime(2024, 12, 23),
            Status = ScheduleStatus.OpenForPreferences,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ShiftSchedules.Add(schedule);
        await context.SaveChangesAsync();

        // Act
        schedule.Status = ScheduleStatus.PendingReview;
        schedule.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.ShiftSchedules.FindAsync(schedule.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(ScheduleStatus.PendingReview);
    }

    [Fact]
    public async Task Schedule_CanTransitionFromFinalizedToArchived()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Week 1",
            WeekStartDate = new DateTime(2024, 12, 23),
            Status = ScheduleStatus.Finalized,
            FinalizedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ShiftSchedules.Add(schedule);
        await context.SaveChangesAsync();

        // Act
        schedule.Status = ScheduleStatus.Archived;
        schedule.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.ShiftSchedules.FindAsync(schedule.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(ScheduleStatus.Archived);
    }

    [Fact]
    public async Task GetByStatus_ReturnsOnlyMatchingSchedules()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        context.ShiftSchedules.Add(new ShiftSchedule { Id = Guid.NewGuid(), Name = "Draft 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        context.ShiftSchedules.Add(new ShiftSchedule { Id = Guid.NewGuid(), Name = "Draft 2", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        context.ShiftSchedules.Add(new ShiftSchedule { Id = Guid.NewGuid(), Name = "Open 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.OpenForPreferences, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act
        var draftSchedules = await context.ShiftSchedules.Where(s => s.Status == ScheduleStatus.Draft).ToListAsync();
        var openSchedules = await context.ShiftSchedules.Where(s => s.Status == ScheduleStatus.OpenForPreferences).ToListAsync();

        // Assert
        draftSchedules.Should().HaveCount(2);
        openSchedules.Should().HaveCount(1);
        openSchedules[0].Name.Should().Be("Open 1");
    }
}
