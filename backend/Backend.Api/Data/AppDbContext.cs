using Backend.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ShiftSchedule> ShiftSchedules => Set<ShiftSchedule>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<EmployeePreference> EmployeePreferences => Set<EmployeePreference>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<ManagerEmployee> ManagerEmployees => Set<ManagerEmployee>();
    public DbSet<ShiftRequest> ShiftRequests => Set<ShiftRequest>();
    public DbSet<TimeOffRequest> TimeOffRequests => Set<TimeOffRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Employee configuration
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Abilities).HasColumnType("jsonb");
        });

        // ShiftSchedule configuration
        modelBuilder.Entity<ShiftSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.HasIndex(e => e.WeekStartDate);
            entity.HasIndex(e => e.Status);
        });

        // Shift configuration
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RequiredAbilities).HasColumnType("jsonb");
            entity.HasIndex(e => e.ShiftScheduleId);
        });

        // EmployeePreference configuration
        modelBuilder.Entity<EmployeePreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.ShiftId);
            entity.HasIndex(e => new { e.EmployeeId, e.ShiftId }).IsUnique();
        });

        // ShiftAssignment configuration
        modelBuilder.Entity<ShiftAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShiftId).IsUnique();
            entity.HasIndex(e => e.EmployeeId);
        });

        // ManagerEmployee configuration (many-to-many)
        modelBuilder.Entity<ManagerEmployee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ManagerId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => new { e.ManagerId, e.EmployeeId }).IsUnique();
        });

        // ShiftRequest configuration
        modelBuilder.Entity<ShiftRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestType)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.ReviewNote).HasMaxLength(500);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.ShiftId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.EmployeeId, e.ShiftId }).IsUnique();
        });

        // TimeOffRequest configuration
        modelBuilder.Entity<TimeOffRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.ReviewNote).HasMaxLength(500);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });
    }
}
