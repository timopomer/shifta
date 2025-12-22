using Backend.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Backend.Tests;

public class PostgresTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("shifta_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private NpgsqlDataSource? _dataSource;
    private DbContextOptions<AppDbContext>? _options;

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        
        // Create a single data source and options to be reused
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        _dataSource = dataSourceBuilder.Build();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dataSource)
            .Options;

        // Ensure database schema is created once
        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    public async Task DisposeAsync()
    {
        _dataSource?.Dispose();
        await _postgres.DisposeAsync();
    }

    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(_options!);
    }
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresTestFixture>
{
}
