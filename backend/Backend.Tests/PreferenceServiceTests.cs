using Backend.Api.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class PreferenceTests
{
    [Fact]
    public async Task Preference_CanBeCreatedForShift()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var schedule = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.OpenForPreferences, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var shift = new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule.Id, Name = "Morning", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var employee = new Employee { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        context.ShiftSchedules.Add(schedule);
        context.Shifts.Add(shift);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        // Act
        var preference = new EmployeePreference
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            ShiftId = shift.Id,
            Type = PreferenceType.PreferShift,
            IsHard = false,
            CreatedAt = DateTime.UtcNow
        };
        context.EmployeePreferences.Add(preference);
        await context.SaveChangesAsync();

        // Assert
        var created = await context.EmployeePreferences.FindAsync(preference.Id);
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.Type.Should().Be(PreferenceType.PreferShift);
    }

    [Fact]
    public async Task Preference_CanBeUnavailablePeriod()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var schedule = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.OpenForPreferences, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var shift = new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule.Id, Name = "Morning", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var employee = new Employee { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        context.ShiftSchedules.Add(schedule);
        context.Shifts.Add(shift);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        var periodStart = new DateTime(2024, 12, 25, 0, 0, 0);
        var periodEnd = new DateTime(2024, 12, 25, 23, 59, 59);

        // Act
        var preference = new EmployeePreference
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            ShiftId = shift.Id,
            Type = PreferenceType.Unavailable,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            IsHard = true,
            CreatedAt = DateTime.UtcNow
        };
        context.EmployeePreferences.Add(preference);
        await context.SaveChangesAsync();

        // Assert
        var created = await context.EmployeePreferences.FindAsync(preference.Id);
        created.Should().NotBeNull();
        created!.Type.Should().Be(PreferenceType.Unavailable);
        created.PeriodStart.Should().Be(periodStart);
        created.PeriodEnd.Should().Be(periodEnd);
        created.IsHard.Should().BeTrue();
    }

    [Fact]
    public async Task GetByScheduleId_ReturnsPreferencesViaShiftJoin()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var schedule1 = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.OpenForPreferences, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var schedule2 = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 2", WeekStartDate = DateTime.Now.AddDays(7), Status = ScheduleStatus.OpenForPreferences, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var shift1 = new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule1.Id, Name = "Shift 1", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var shift2 = new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule2.Id, Name = "Shift 2", StartTime = DateTime.Now.AddDays(7), EndTime = DateTime.Now.AddDays(7).AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var employee = new Employee { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        context.ShiftSchedules.AddRange(schedule1, schedule2);
        context.Shifts.AddRange(shift1, shift2);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        context.EmployeePreferences.Add(new EmployeePreference { Id = Guid.NewGuid(), EmployeeId = employee.Id, ShiftId = shift1.Id, Type = PreferenceType.PreferShift, CreatedAt = DateTime.UtcNow });
        context.EmployeePreferences.Add(new EmployeePreference { Id = Guid.NewGuid(), EmployeeId = employee.Id, ShiftId = shift2.Id, Type = PreferenceType.PreferShift, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act - Using explicit join
        var schedule1Prefs = await (
            from preference in context.EmployeePreferences
            join shift in context.Shifts on preference.ShiftId equals shift.Id
            where shift.ShiftScheduleId == schedule1.Id
            select preference
        ).ToListAsync();

        var schedule2Prefs = await (
            from preference in context.EmployeePreferences
            join shift in context.Shifts on preference.ShiftId equals shift.Id
            where shift.ShiftScheduleId == schedule2.Id
            select preference
        ).ToListAsync();

        // Assert
        schedule1Prefs.Should().HaveCount(1);
        schedule1Prefs[0].ShiftId.Should().Be(shift1.Id);
        schedule2Prefs.Should().HaveCount(1);
        schedule2Prefs[0].ShiftId.Should().Be(shift2.Id);
    }

    [Fact]
    public async Task GetByEmployeeId_ReturnsAllEmployeePreferences()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var schedule = new ShiftSchedule { Id = Guid.NewGuid(), Name = "Week 1", WeekStartDate = DateTime.Now, Status = ScheduleStatus.OpenForPreferences, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var shift1 = new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule.Id, Name = "Morning", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(6), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var shift2 = new Shift { Id = Guid.NewGuid(), ShiftScheduleId = schedule.Id, Name = "Evening", StartTime = DateTime.Now.AddHours(12), EndTime = DateTime.Now.AddHours(18), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var alice = new Employee { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var bob = new Employee { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        context.ShiftSchedules.Add(schedule);
        context.Shifts.AddRange(shift1, shift2);
        context.Employees.AddRange(alice, bob);
        await context.SaveChangesAsync();

        context.EmployeePreferences.Add(new EmployeePreference { Id = Guid.NewGuid(), EmployeeId = alice.Id, ShiftId = shift1.Id, Type = PreferenceType.PreferShift, CreatedAt = DateTime.UtcNow });
        context.EmployeePreferences.Add(new EmployeePreference { Id = Guid.NewGuid(), EmployeeId = alice.Id, ShiftId = shift2.Id, Type = PreferenceType.PreferShift, CreatedAt = DateTime.UtcNow });
        context.EmployeePreferences.Add(new EmployeePreference { Id = Guid.NewGuid(), EmployeeId = bob.Id, ShiftId = shift1.Id, Type = PreferenceType.PreferShift, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act
        var alicePrefs = await context.EmployeePreferences.Where(p => p.EmployeeId == alice.Id).ToListAsync();
        var bobPrefs = await context.EmployeePreferences.Where(p => p.EmployeeId == bob.Id).ToListAsync();

        // Assert
        alicePrefs.Should().HaveCount(2);
        bobPrefs.Should().HaveCount(1);
    }
}
