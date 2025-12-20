using Backend.Api.Entities;
using Backend.Api.Services;
using FluentAssertions;

namespace Backend.Tests;

public class EmployeeServiceTests
{
    [Fact]
    public async Task Employee_CanBeCreatedWithAbilities()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new EmployeeService(context);

        var employee = new Employee
        {
            Name = "Alice",
            Email = "alice@example.com",
            Abilities = ["bartender", "waiter", "kitchen"],
            IsManager = false
        };

        // Act
        var created = await service.CreateAsync(employee);

        // Assert
        created.Id.Should().NotBeEmpty();
        created.Abilities.Should().HaveCount(3);
        created.Abilities.Should().Contain("bartender");
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Employee_CanBeManager()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new EmployeeService(context);

        var manager = new Employee
        {
            Name = "Bob",
            Email = "bob@example.com",
            Abilities = ["bartender"],
            IsManager = true
        };

        // Act
        var created = await service.CreateAsync(manager);

        // Assert
        created.IsManager.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ModifiesEmployee()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new EmployeeService(context);

        var employee = await service.CreateAsync(new Employee
        {
            Name = "Alice",
            Email = "alice@example.com",
            Abilities = ["waiter"]
        });

        // Act
        employee.Name = "Alice Smith";
        employee.Abilities = ["waiter", "bartender"];
        var updated = await service.UpdateAsync(employee);

        // Assert
        updated.Name.Should().Be("Alice Smith");
        updated.Abilities.Should().HaveCount(2);
        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [Fact]
    public async Task Delete_RemovesEmployee()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new EmployeeService(context);

        var employee = await service.CreateAsync(new Employee
        {
            Name = "Alice",
            Email = "alice@example.com"
        });

        // Act
        await service.DeleteAsync(employee.Id);
        var retrieved = await service.GetByIdAsync(employee.Id);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ReturnsAllEmployees()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new EmployeeService(context);

        await service.CreateAsync(new Employee { Name = "Alice", Email = "alice@example.com" });
        await service.CreateAsync(new Employee { Name = "Bob", Email = "bob@example.com" });
        await service.CreateAsync(new Employee { Name = "Carol", Email = "carol@example.com" });

        // Act
        var all = await service.GetAllAsync();

        // Assert
        all.Should().HaveCount(3);
    }
}
