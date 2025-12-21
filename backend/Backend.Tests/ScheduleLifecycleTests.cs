using Backend.Api.Data;
using Backend.Api.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

[Collection("Postgres")]
public class ScheduleLifecycleTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private AppDbContext _context = null!;

    public ScheduleLifecycleTests(PostgresTestFixture fixture)
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
    public async Task Schedule_StartsInDraftStatus()
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

        // Act
        _context.ShiftSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Assert
        var created = await _context.ShiftSchedules.FindAsync(schedule.Id);
        created.Should().NotBeNull();
        created!.Status.Should().Be(ScheduleStatus.Draft);
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        created.FinalizedAt.Should().BeNull();
    }

    [Fact]
    public async Task Schedule_CanTransitionFromDraftToOpenForPreferences()
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

        // Add a shift (required to open for preferences)
        _context.Shifts.Add(new Shift
        {
            Id = Guid.NewGuid(),
            ShiftScheduleId = schedule.Id,
            Name = "Morning",
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddHours(6),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        schedule.Status = ScheduleStatus.OpenForPreferences;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.ShiftSchedules.FindAsync(schedule.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(ScheduleStatus.OpenForPreferences);
    }

    [Fact]
    public async Task Schedule_CanTransitionFromOpenForPreferencesToPendingReview()
    {
        // Arrange
        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Week 1",
            WeekStartDate = new DateTime(2024, 12, 23),
            Status = ScheduleStatus.OpenForPreferences,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ShiftSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        schedule.Status = ScheduleStatus.PendingReview;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.ShiftSchedules.FindAsync(schedule.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(ScheduleStatus.PendingReview);
    }

    [Fact]
    public async Task Schedule_CanTransitionFromFinalizedToArchived()
    {
        // Arrange
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
        _context.ShiftSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        schedule.Status = ScheduleStatus.Archived;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.ShiftSchedules.FindAsync(schedule.Id);
        updated.Should().NotBeNull();
        updated!.Status.Should().Be(ScheduleStatus.Archived);
    }

    [Fact]
    public async Task GetByStatus_ReturnsOnlyMatchingSchedules()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        _context.ShiftSchedules.Add(new ShiftSchedule { Id = id1, Name = "Draft 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _context.ShiftSchedules.Add(new ShiftSchedule { Id = id2, Name = "Draft 2", WeekStartDate = DateTime.Now, Status = ScheduleStatus.Draft, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _context.ShiftSchedules.Add(new ShiftSchedule { Id = id3, Name = "Open 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.OpenForPreferences, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var draftSchedules = await _context.ShiftSchedules.Where(s => s.Id == id1 || s.Id == id2).ToListAsync();
        var openSchedules = await _context.ShiftSchedules.Where(s => s.Id == id3).ToListAsync();

        // Assert
        draftSchedules.Should().HaveCount(2);
        openSchedules.Should().HaveCount(1);
        openSchedules[0].Name.Should().Be("Open 1");
    }
}
