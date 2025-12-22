using Backend.Api.Data;
using Backend.Api.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

[Collection("Postgres")]
public class EmployeeTests : IAsyncLifetime
{
    private readonly PostgresTestFixture _fixture;
    private AppDbContext _context = null!;

    public EmployeeTests(PostgresTestFixture fixture)
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
        _context.Employees.RemoveRange(_context.Employees);
        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Employee_CanBeCreatedWithAbilities()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = $"alice-{Guid.NewGuid()}@example.com",
            Abilities = ["bartender", "waiter", "kitchen"],
            IsManager = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Assert
        var created = await _context.Employees.FindAsync(employee.Id);
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
        var manager = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Bob",
            Email = $"bob-{Guid.NewGuid()}@example.com",
            Abilities = ["bartender"],
            IsManager = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Employees.Add(manager);
        await _context.SaveChangesAsync();

        // Assert
        var created = await _context.Employees.FindAsync(manager.Id);
        created.Should().NotBeNull();
        created!.IsManager.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ModifiesEmployee()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = $"alice-{Guid.NewGuid()}@example.com",
            Abilities = ["waiter"],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Act
        employee.Name = "Alice Smith";
        employee.Abilities = ["waiter", "bartender"];
        employee.UpdatedAt = DateTime.UtcNow.AddSeconds(1);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Employees.FindAsync(employee.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Alice Smith");
        updated.Abilities.Should().HaveCount(2);
        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [Fact]
    public async Task Delete_RemovesEmployee()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            Name = "Alice",
            Email = $"alice-{Guid.NewGuid()}@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Act
        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
        var retrieved = await _context.Employees.FindAsync(employee.Id);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ReturnsAllEmployees()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        _context.Employees.Add(new Employee { Id = id1, Name = "Alice", Email = $"alice-{id1}@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _context.Employees.Add(new Employee { Id = id2, Name = "Bob", Email = $"bob-{id2}@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _context.Employees.Add(new Employee { Id = id3, Name = "Carol", Email = $"carol-{id3}@example.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        // Act
        var all = await _context.Employees.Where(e => e.Id == id1 || e.Id == id2 || e.Id == id3).ToListAsync();

        // Assert
        all.Should().HaveCount(3);
    }
}
