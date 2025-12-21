using Backend.Api.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

public class EmployeeTests
{
    [Fact]
    public async Task Employee_CanBeCreatedWithAbilities()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            Abilities = ["bartender", "waiter", "kitchen"],
            IsManager = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        // Assert
        var created = await context.Employees.FindAsync(employee.Id);
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.Abilities.Should().HaveCount(3);
        created.Abilities.Should().Contain("bartender");
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Employee_CanBeManager()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var manager = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Bob",
            Email = "bob@example.com",
            Abilities = ["bartender"],
            IsManager = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Employees.Add(manager);
        await context.SaveChangesAsync();

        // Assert
        var created = await context.Employees.FindAsync(manager.Id);
        created.Should().NotBeNull();
        created!.IsManager.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ModifiesEmployee()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            Abilities = ["waiter"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        // Act
        employee.Name = "Alice Smith";
        employee.Abilities = ["waiter", "bartender"];
        employee.UpdatedAt = DateTime.UtcNow.AddSeconds(1);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Employees.FindAsync(employee.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Alice Smith");
        updated.Abilities.Should().HaveCount(2);
        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [Fact]
    public async Task Delete_RemovesEmployee()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = "alice@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        // Act
        context.Employees.Remove(employee);
        await context.SaveChangesAsync();
        var retrieved = await context.Employees.FindAsync(employee.Id);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ReturnsAllEmployees()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        context.Employees.Add(new Employee { Id = Guid.NewGuid(), Name = "Alice", Email = "alice@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        context.Employees.Add(new Employee { Id = Guid.NewGuid(), Name = "Bob", Email = "bob@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        context.Employees.Add(new Employee { Id = Guid.NewGuid(), Name = "Carol", Email = "carol@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act
        var all = await context.Employees.ToListAsync();

        // Assert
        all.Should().HaveCount(3);
    }
}
