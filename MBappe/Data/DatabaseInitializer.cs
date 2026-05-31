using MBappe.Models;
using MBappe.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MBappe.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync()
    {
        await using var db = new AppDbContext();

        await db.Database.EnsureCreatedAsync();

        await SeedUsersAsync(db);
        await SeedMotivationProgramsAsync(db);
        //await SeedLearningCoursesAsync(db);

        await db.SaveChangesAsync();

        Debug.WriteLine("Database initialized.");
        Debug.WriteLine($"Database path: {db.Database.GetDbConnection().DataSource}");
    }

    private static async Task SeedUsersAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        AddUser(db, "employee", "employee@mbappe.local", "Иван Петров", "12345", UserRole.Employee);
        AddUser(db, "manager", "manager@mbappe.local", "Анна Смирнова", "12345", UserRole.Manager);
        AddUser(db, "hr", "hr@mbappe.local", "Мария HR", "12345", UserRole.HrSpecialist);
        AddUser(db, "admin", "admin@mbappe.local", "Администратор", "12345", UserRole.Administrator);
    }

    private static async Task SeedMotivationProgramsAsync(AppDbContext db)
    {
        if (await db.MotivationPrograms.AnyAsync())
            return;

        db.MotivationPrograms.Add(new MotivationProgram
        {
            Title = "Ежемесячная премия по KPI",
            Description = "Базовая программа премирования сотрудников по результатам выполнения KPI.",
            BaseAmount = 10_000m,
            MinEfficiencyPercent = 60,
            MaxEfficiencyPercent = 120,
            IsActive = true,
            CreatedByUserId = Guid.Empty,
            CreatedAt = DateTime.Now
        });
    }

    //private static async Task SeedLearningCoursesAsync(AppDbContext db)
    //{
    //    if (await db.LearningCourses.AnyAsync())
    //        return;

    //    db.LearningCourses.Add(new LearningCourse
    //    {
    //        Title = "Введение в корпоративную культуру",
    //        Description = "Базовый курс для новых сотрудников.",
    //        DurationHours = 2,
    //        IsActive = true,
    //        CreatedAt = DateTime.Now
    //    });

    //    db.LearningCourses.Add(new LearningCourse
    //    {
    //        Title = "Основы информационной безопасности",
    //        Description = "Курс по базовым правилам защиты информации.",
    //        DurationHours = 3,
    //        IsActive = true,
    //        CreatedAt = DateTime.Now
    //    });

    //    db.LearningCourses.Add(new LearningCourse
    //    {
    //        Title = "Работа с KPI",
    //        Description = "Курс по пониманию и выполнению показателей эффективности.",
    //        DurationHours = 1.5,
    //        IsActive = true,
    //        CreatedAt = DateTime.Now
    //    });
    //}

    private static void AddUser(
        AppDbContext db,
        string login,
        string email,
        string fullName,
        string password,
        UserRole role)
    {
        var hasher = new PasswordHasher();
        var salt = hasher.GenerateSalt();
        var hash = hasher.HashPassword(password, salt);

        db.Users.Add(new AppUser
        {
            Login = login,
            Email = email,
            FullName = fullName,
            PasswordSalt = salt,
            PasswordHash = hash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.Now
        });
    }
}