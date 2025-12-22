using Backend.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Backend.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private NpgsqlDataSource? _dataSource;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
        
        // Create a single data source to be reused
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
        dataSourceBuilder.EnableDynamicJson();
        _dataSource = dataSourceBuilder.Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Remove existing NpgsqlDataSource if registered
            var npgsqlDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(NpgsqlDataSource));

            if (npgsqlDescriptor != null)
            {
                services.Remove(npgsqlDescriptor);
            }

            // Add PostgreSQL with the shared data source
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_dataSource!);
            });

            // Build service provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dataSource?.Dispose();
        }
        base.Dispose(disposing);
    }
}
