using Backend.Api.Clients.Generated;
using Backend.Api.Controllers;
using Backend.Api.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class OptimizerClientTests
{
    [Fact]
    public async Task Finalize_WithMockedOptimizer_ReturnsScheduleWithAssignments()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var optimizerClient = Substitute.For<IOptimizerApiClient>();
        var logger = NullLogger<SchedulesController>.Instance;
        var controller = new SchedulesController(context, optimizerClient, logger);

        // Create employees
        var employee1 = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            Abilities = ["bartender"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var employee2 = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Bob",
            Email = "bob@example.com",
            Abilities = ["bartender"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Employees.AddRange(employee1, employee2);

        // Create schedule
        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Test Week",
            WeekStartDate = DateTime.UtcNow,
            Status = ScheduleStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ShiftSchedules.Add(schedule);

        // Create shifts
        var shift1 = new Shift
        {
            Id = Guid.NewGuid(),
            ShiftScheduleId = schedule.Id,
            Name = "Morning",
            StartTime = DateTime.UtcNow.AddHours(8),
            EndTime = DateTime.UtcNow.AddHours(14),
            RequiredAbilities = ["bartender"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var shift2 = new Shift
        {
            Id = Guid.NewGuid(),
            ShiftScheduleId = schedule.Id,
            Name = "Evening",
            StartTime = DateTime.UtcNow.AddHours(14),
            EndTime = DateTime.UtcNow.AddHours(22),
            RequiredAbilities = ["bartender"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Shifts.AddRange(shift1, shift2);
        await context.SaveChangesAsync();

        // Mock optimizer to return a solution with assignments
        var mockResponse = new OptimizeResponse
        {
            Success = true,
            Solutions =
            [
                new SolutionDto
                {
                    Assignments = new Dictionary<string, string>
                    {
                        { shift1.Id.ToString(), employee1.Id.ToString() },
                        { shift2.Id.ToString(), employee2.Id.ToString() }
                    },
                    Metrics = new SolutionMetricsDto
                    {
                        Soft_preference_score = 100,
                        Fairness_score = 1.0,
                        Total_shifts_assigned = 2,
                        Preferences_satisfied = new Dictionary<string, int>
                        {
                            { employee1.Id.ToString(), 1 },
                            { employee2.Id.ToString(), 1 }
                        }
                    }
                }
            ]
        };

        optimizerClient
            .PostAsync(Arg.Any<OptimizeRequest>(), Arg.Any<CancellationToken>())
            .Returns(mockResponse);

        // Act
        var result = await controller.Finalize(schedule.Id, CancellationToken.None);

        // Assert
        var okResult = result.Value;
        okResult.Should().NotBeNull();
        okResult!.Status.Should().Be(ScheduleStatus.Finalized);

        var assignments = await context.ShiftAssignments
            .Where(a => context.Shifts.Where(s => s.ShiftScheduleId == schedule.Id).Select(s => s.Id).Contains(a.ShiftId))
            .ToListAsync();
        assignments.Should().HaveCount(2);
        assignments.Should().Contain(a => a.ShiftId == shift1.Id && a.EmployeeId == employee1.Id);
        assignments.Should().Contain(a => a.ShiftId == shift2.Id && a.EmployeeId == employee2.Id);
    }

    [Fact]
    public async Task Finalize_WithMockedOptimizer_ReturnsMultipleSolutions()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var optimizerClient = Substitute.For<IOptimizerApiClient>();
        var logger = NullLogger<SchedulesController>.Instance;
        var controller = new SchedulesController(context, optimizerClient, logger);

        // Create employees
        var employee1 = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            Abilities = ["bartender"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var employee2 = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Bob",
            Email = "bob@example.com",
            Abilities = ["bartender"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Employees.AddRange(employee1, employee2);

        // Create schedule
        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Test Week",
            WeekStartDate = DateTime.UtcNow,
            Status = ScheduleStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ShiftSchedules.Add(schedule);

        // Create a single shift that could be assigned to either employee
        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            ShiftScheduleId = schedule.Id,
            Name = "Morning",
            StartTime = DateTime.UtcNow.AddHours(8),
            EndTime = DateTime.UtcNow.AddHours(14),
            RequiredAbilities = ["bartender"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Shifts.Add(shift);
        await context.SaveChangesAsync();

        // Mock optimizer to return multiple possible solutions
        var mockResponse = new OptimizeResponse
        {
            Success = true,
            Solutions =
            [
                // Best solution - Alice gets the shift (higher preference score)
                new SolutionDto
                {
                    Assignments = new Dictionary<string, string>
                    {
                        { shift.Id.ToString(), employee1.Id.ToString() }
                    },
                    Metrics = new SolutionMetricsDto
                    {
                        Soft_preference_score = 100,
                        Fairness_score = 1.0,
                        Total_shifts_assigned = 1
                    }
                },
                // Alternative solution - Bob gets the shift
                new SolutionDto
                {
                    Assignments = new Dictionary<string, string>
                    {
                        { shift.Id.ToString(), employee2.Id.ToString() }
                    },
                    Metrics = new SolutionMetricsDto
                    {
                        Soft_preference_score = 80,
                        Fairness_score = 0.9,
                        Total_shifts_assigned = 1
                    }
                },
                // Third solution - could represent different trade-offs
                new SolutionDto
                {
                    Assignments = new Dictionary<string, string>
                    {
                        { shift.Id.ToString(), employee1.Id.ToString() }
                    },
                    Metrics = new SolutionMetricsDto
                    {
                        Soft_preference_score = 90,
                        Fairness_score = 0.95,
                        Total_shifts_assigned = 1
                    }
                }
            ]
        };

        optimizerClient
            .PostAsync(Arg.Any<OptimizeRequest>(), Arg.Any<CancellationToken>())
            .Returns(mockResponse);

        // Act
        var result = await controller.Finalize(schedule.Id, CancellationToken.None);

        // Assert - should pick the first (best) solution
        var okResult = result.Value;
        okResult.Should().NotBeNull();
        okResult!.Status.Should().Be(ScheduleStatus.Finalized);

        var assignments = await context.ShiftAssignments
            .Where(a => a.ShiftId == shift.Id)
            .ToListAsync();
        assignments.Should().HaveCount(1);
        // First solution assigns to employee1
        assignments[0].EmployeeId.Should().Be(employee1.Id);

        // Verify the optimizer was called with correct request structure
        await optimizerClient.Received(1).PostAsync(
            Arg.Is<OptimizeRequest>(r =>
                r.Employees.Count == 2 &&
                r.Shifts.Count == 1 &&
                r.Max_solutions == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Finalize_WhenOptimizerFails_ReturnsBadRequest()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var optimizerClient = Substitute.For<IOptimizerApiClient>();
        var logger = NullLogger<SchedulesController>.Instance;
        var controller = new SchedulesController(context, optimizerClient, logger);

        // Create minimal data for the test
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            Abilities = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Employees.Add(employee);

        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Test Week",
            WeekStartDate = DateTime.UtcNow,
            Status = ScheduleStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ShiftSchedules.Add(schedule);

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            ShiftScheduleId = schedule.Id,
            Name = "Morning",
            StartTime = DateTime.UtcNow.AddHours(8),
            EndTime = DateTime.UtcNow.AddHours(14),
            RequiredAbilities = ["special_skill"], // Employee doesn't have this
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Shifts.Add(shift);
        await context.SaveChangesAsync();

        // Mock optimizer to return failure (no valid solution found)
        var errorObj = new Error();
        errorObj.AdditionalProperties["error"] = "No feasible solution found: no employee has required abilities";
        
        var mockResponse = new OptimizeResponse
        {
            Success = false,
            Error = errorObj,
            Solutions = []
        };

        optimizerClient
            .PostAsync(Arg.Any<OptimizeRequest>(), Arg.Any<CancellationToken>())
            .Returns(mockResponse);

        // Act
        var result = await controller.Finalize(schedule.Id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
    }

    [Fact]
    public async Task Finalize_WhenOptimizerReturnsEmptySolutions_ReturnsBadRequest()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var optimizerClient = Substitute.For<IOptimizerApiClient>();
        var logger = NullLogger<SchedulesController>.Instance;
        var controller = new SchedulesController(context, optimizerClient, logger);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            Abilities = ["bartender"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Employees.Add(employee);

        var schedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Test Week",
            WeekStartDate = DateTime.UtcNow,
            Status = ScheduleStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ShiftSchedules.Add(schedule);

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            ShiftScheduleId = schedule.Id,
            Name = "Morning",
            StartTime = DateTime.UtcNow.AddHours(8),
            EndTime = DateTime.UtcNow.AddHours(14),
            RequiredAbilities = ["bartender"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Shifts.Add(shift);
        await context.SaveChangesAsync();

        // Mock optimizer returns success but empty solutions
        var mockResponse = new OptimizeResponse
        {
            Success = true,
            Solutions = []
        };

        optimizerClient
            .PostAsync(Arg.Any<OptimizeRequest>(), Arg.Any<CancellationToken>())
            .Returns(mockResponse);

        // Act
        var result = await controller.Finalize(schedule.Id, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
    }
}
