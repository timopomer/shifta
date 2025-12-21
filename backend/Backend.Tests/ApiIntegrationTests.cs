using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using FluentAssertions;

namespace Backend.Tests;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task GetEmployees_ReturnsEmptyList_WhenNoEmployees()
    {
        // Act
        var response = await _client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var employees = await response.Content.ReadFromJsonAsync<List<EmployeeResponse>>();
        employees.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateEmployee_ReturnsCreatedEmployee()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            Name = "Test Employee",
            Email = $"test-{Guid.NewGuid()}@example.com",
            Abilities = ["bartender", "waiter"],
            IsManager = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/employees", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var employee = await response.Content.ReadFromJsonAsync<EmployeeResponse>();
        employee.Should().NotBeNull();
        employee!.Name.Should().Be("Test Employee");
        employee.Abilities.Should().HaveCount(2);
        employee.Abilities.Should().Contain("bartender");
        employee.Abilities.Should().Contain("waiter");
    }

    [Fact]
    public async Task CreateEmployee_WithEmptyAbilities_Succeeds()
    {
        // Arrange
        var request = new CreateEmployeeRequest
        {
            Name = "No Skills Employee",
            Email = $"noskills-{Guid.NewGuid()}@example.com",
            Abilities = [],
            IsManager = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/employees", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var employee = await response.Content.ReadFromJsonAsync<EmployeeResponse>();
        employee.Should().NotBeNull();
        employee!.Abilities.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSchedules_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/schedules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record HealthResponse(string Status);
}
