using Backend.Api.Entities;
using Backend.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Backend.Api.Clients;

namespace Backend.Tests;

public class ScheduleLifecycleTests
{
    [Fact]
    public async Task Schedule_StartsInDraftStatus()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = CreateScheduleService(context);

        var schedule = new ShiftSchedule
        {
            Name = "Week 1",
            WeekStartDate = new DateTime(2024, 12, 23)
        };

        // Act
        var created = await service.CreateAsync(schedule);

        // Assert
        created.Status.Should().Be(ScheduleStatus.Draft);
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        created.FinalizedAt.Should().BeNull();
    }

    [Fact]
    public async Task Schedule_CanTransitionFromDraftToOpenForPreferences()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = CreateScheduleService(context);

        var schedule = await service.CreateAsync(new ShiftSchedule
        {
            Name = "Week 1",
            WeekStartDate = new DateTime(2024, 12, 23)
        });

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
        var updated = await service.OpenForPreferencesAsync(schedule.Id);

        // Assert
        updated.Status.Should().Be(ScheduleStatus.OpenForPreferences);
    }

    [Fact]
    public async Task Schedule_CanTransitionFromOpenForPreferencesToPendingReview()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = CreateScheduleService(context);

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
        var updated = await service.ClosePreferencesAsync(schedule.Id);

        // Assert
        updated.Status.Should().Be(ScheduleStatus.PendingReview);
    }

    [Fact]
    public async Task Schedule_CanTransitionFromFinalizedToArchived()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = CreateScheduleService(context);

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
        var updated = await service.ArchiveAsync(schedule.Id);

        // Assert
        updated.Status.Should().Be(ScheduleStatus.Archived);
    }

    [Fact]
    public async Task GetByStatus_ReturnsOnlyMatchingSchedules()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = CreateScheduleService(context);

        await service.CreateAsync(new ShiftSchedule { Name = "Draft 1", WeekStartDate = DateTime.Now });
        await service.CreateAsync(new ShiftSchedule { Name = "Draft 2", WeekStartDate = DateTime.Now });

        var openSchedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Open 1",
            WeekStartDate = DateTime.Now,
            Status = ScheduleStatus.OpenForPreferences,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ShiftSchedules.Add(openSchedule);
        await context.SaveChangesAsync();

        // Act
        var draftSchedules = await service.GetByStatusAsync(ScheduleStatus.Draft);
        var openSchedules = await service.GetByStatusAsync(ScheduleStatus.OpenForPreferences);

        // Assert
        draftSchedules.Should().HaveCount(2);
        openSchedules.Should().HaveCount(1);
        openSchedules[0].Name.Should().Be("Open 1");
    }

    private static ScheduleService CreateScheduleService(Api.Data.AppDbContext context)
    {
        var optimizerClient = Substitute.For<IOptimizerClient>();
        var logger = NullLogger<ScheduleService>.Instance;
        return new ScheduleService(context, optimizerClient, logger);
    }
}
