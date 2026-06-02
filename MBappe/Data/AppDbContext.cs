using MBappe.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace MBappe.Data;

public class AppDbContext : DbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<EmployeeProfile> Employees => Set<EmployeeProfile>();

    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    public DbSet<KpiItem> Kpis => Set<KpiItem>();

    public DbSet<LearningCourse> LearningCourses => Set<LearningCourse>();

    public DbSet<LearningAssignment> LearningAssignments => Set<LearningAssignment>();

    public DbSet<MotivationProgram> MotivationPrograms => Set<MotivationProgram>();

    public DbSet<MotivationBonus> MotivationBonuses => Set<MotivationBonus>();
    
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        var databaseFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MBappe");

        Directory.CreateDirectory(databaseFolder);

        var databasePath = Path.Combine(databaseFolder, "mbappe.db");

        optionsBuilder.UseSqlite($"Data Source={databasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureEmployees(modelBuilder);
        ConfigureKpis(modelBuilder);
        ConfigureLearning(modelBuilder);
        ConfigureMotivation(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasKey(user => user.Id);

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.Login)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .Property(user => user.Login)
            .IsRequired();

        modelBuilder.Entity<AppUser>()
            .Property(user => user.Email)
            .IsRequired();

        modelBuilder.Entity<AppUser>()
            .Property(user => user.FullName)
            .IsRequired();
    }

    private static void ConfigureEmployees(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeProfile>()
            .HasKey(employee => employee.Id);

        modelBuilder.Entity<EmployeeProfile>()
            .HasIndex(employee => employee.UserId)
            .IsUnique();

        modelBuilder.Entity<EmployeeProfile>()
            .HasIndex(employee => employee.PersonnelNumber)
            .IsUnique();

        modelBuilder.Entity<EmployeeProfile>()
            .Property(employee => employee.PersonnelNumber)
            .IsRequired();

        modelBuilder.Entity<EmployeeProfile>()
            .Property(employee => employee.FullName)
            .IsRequired();

        modelBuilder.Entity<EmployeeProfile>()
            .Property(employee => employee.Position)
            .IsRequired();

        modelBuilder.Entity<EmployeeProfile>()
            .Property(employee => employee.Department)
            .IsRequired();
    }

    private static void ConfigureKpis(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KpiItem>()
            .HasKey(kpi => kpi.Id);

        modelBuilder.Entity<KpiItem>()
            .Ignore(kpi => kpi.CompletionPercent);

        modelBuilder.Entity<KpiItem>()
            .Ignore(kpi => kpi.CappedCompletionPercent);

        modelBuilder.Entity<KpiItem>()
            .Ignore(kpi => kpi.IsOverfulfilled);

        modelBuilder.Entity<KpiItem>()
            .Property(kpi => kpi.Title)
            .IsRequired();

        modelBuilder.Entity<KpiItem>()
            .Property(kpi => kpi.Unit)
            .IsRequired();
    }

    private static void ConfigureLearning(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearningCourse>()
            .HasKey(course => course.Id);

        modelBuilder.Entity<LearningAssignment>()
            .HasKey(assignment => assignment.Id);
    }

    private static void ConfigureMotivation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MotivationProgram>()
            .HasKey(program => program.Id);

        modelBuilder.Entity<MotivationProgram>()
            .Property(program => program.Title)
            .IsRequired();

        modelBuilder.Entity<MotivationProgram>()
            .Property(program => program.BaseAmount)
            .HasConversion<double>();

        modelBuilder.Entity<MotivationBonus>()
            .HasKey(bonus => bonus.Id);

        modelBuilder.Entity<MotivationBonus>()
            .Property(bonus => bonus.BaseAmount)
            .HasConversion<double>();

        modelBuilder.Entity<MotivationBonus>()
            .Property(bonus => bonus.CalculatedAmount)
            .HasConversion<double>();

        modelBuilder.Entity<MotivationBonus>()
            .Property(bonus => bonus.FinalAmount)
            .HasConversion<double>();
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntry>()
            .HasKey(entry => entry.Id);

        modelBuilder.Entity<AuditLogEntry>()
            .Property(entry => entry.Message)
            .IsRequired();
    }
}