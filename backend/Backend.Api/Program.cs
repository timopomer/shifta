using System.Text.Json.Serialization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Backend.Api.Clients.Generated;
using Backend.Api.Data;
using Backend.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Use Autofac as the DI container
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register services - they will automatically get DbContext injected
    containerBuilder.RegisterType<EmployeeService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<ScheduleService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<ShiftService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<PreferenceService>().InstancePerLifetimeScope();
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add generated optimizer client with configured base URL
var optimizerBaseUrl = builder.Configuration["Optimizer:BaseUrl"]
    ?? "http://localhost:8000";
builder.Services.AddHttpClient<IOptimizerApiClient, OptimizerApiClient>(client =>
{
    client.BaseAddress = new Uri(optimizerBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
