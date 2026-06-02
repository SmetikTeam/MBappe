using MBappe.Models;
using MBappe.Repositories;
using MBappe.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace MBappe.Tests.Integration;

[TestFixture]
public class EfRepositoryIntegrationTests
{
    [Test]
    public async Task EfUserRepository_AddAndGetByLogin_PersistsUserInSqlite()
    {
        using var database = new SqliteTestDatabase();

        var repository = new EfUserRepository(database.CreateContext);

        var user = new AppUser
        {
            Login = "sqlite-user",
            Email = "sqlite-user@mbappe.local",
            FullName = "SQLite User",
            PasswordHash = [1, 2, 3],
            PasswordSalt = [4, 5, 6],
            Role = UserRole.Employee,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await repository.AddAsync(user);

        var secondRepository = new EfUserRepository(database.CreateContext);
        var savedUser = await secondRepository.GetByLoginAsync("sqlite-user");

        Assert.That(savedUser, Is.Not.Null);
        Assert.That(savedUser!.Email, Is.EqualTo("sqlite-user@mbappe.local"));
        Assert.That(savedUser.FullName, Is.EqualTo("SQLite User"));
    }

    [Test]
    public async Task EfEmployeeRepository_AddAndGetByUserId_PersistsEmployeeInSqlite()
    {
        using var database = new SqliteTestDatabase();

        var repository = new EfEmployeeRepository(database.CreateContext);

        var userId = Guid.NewGuid();

        var employee = new EmployeeProfile
        {
            UserId = userId,
            PersonnelNumber = "EMP-SQL-001",
            FullName = "SQLite Employee",
            Position = "Developer",
            Department = "IT",
            Email = "employee@mbappe.local",
            Phone = "+7 000 000-00-01",
            HireDate = DateTime.Today,
            Status = EmployeeStatus.Active,
            CreatedAt = DateTime.Now
        };

        await repository.AddAsync(employee);

        var secondRepository = new EfEmployeeRepository(database.CreateContext);
        var savedEmployee = await secondRepository.GetByUserIdAsync(userId);

        Assert.That(savedEmployee, Is.Not.Null);
        Assert.That(savedEmployee!.PersonnelNumber, Is.EqualTo("EMP-SQL-001"));
        Assert.That(savedEmployee.FullName, Is.EqualTo("SQLite Employee"));
    }

    [Test]
    public async Task EfKpiRepository_UpdateAsync_PersistsUpdatedActualValue()
    {
        using var database = new SqliteTestDatabase();

        var repository = new EfKpiRepository(database.CreateContext);

        var kpi = new KpiItem
        {
            EmployeeId = Guid.NewGuid(),
            Title = "Закрыть задачи",
            Description = "Проверка сохранения KPI в SQLite",
            TargetValue = 20,
            ActualValue = 10,
            Unit = "задач",
            WeightPercent = 100,
            PeriodStart = new DateTime(2026, 6, 1),
            PeriodEnd = new DateTime(2026, 6, 30),
            Status = KpiStatus.InProgress,
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.Now
        };

        await repository.AddAsync(kpi);

        kpi.ActualValue = 20;
        kpi.Status = KpiStatus.Completed;
        kpi.UpdatedAt = DateTime.Now;

        await repository.UpdateAsync(kpi);

        var secondRepository = new EfKpiRepository(database.CreateContext);
        var savedKpi = await secondRepository.GetByIdAsync(kpi.Id);

        Assert.That(savedKpi, Is.Not.Null);
        Assert.That(savedKpi!.ActualValue, Is.EqualTo(20));
        Assert.That(savedKpi.Status, Is.EqualTo(KpiStatus.Completed));
        Assert.That(savedKpi.CompletionPercent, Is.EqualTo(100));
    }

    [Test]
    public async Task EfLearningRepository_AddAssignmentAndQueryByEmployee_PersistsAssignment()
    {
        using var database = new SqliteTestDatabase();

        var repository = new EfLearningRepository(database.CreateContext);

        var course = new LearningCourse
        {
            Title = "SQLite Course",
            Description = "Курс для интеграционного теста",
            Format = LearningFormat.Online,
            Provider = "MBappe Academy",
            DurationHours = 2,
            Status = LearningCourseStatus.Active,
            CreatedAt = DateTime.Now
        };

        await repository.AddCourseAsync(course);

        var employeeId = Guid.NewGuid();

        var assignment = new LearningAssignment
        {
            CourseId = course.Id,
            EmployeeId = employeeId,
            AssignedByUserId = Guid.NewGuid(),
            AssignedAt = DateTime.Now,
            DueDate = DateTime.Today.AddDays(7),
            ProgressPercent = 40,
            Status = LearningAssignmentStatus.InProgress,
            CreatedAt = DateTime.Now
        };

        await repository.AddAssignmentAsync(assignment);

        var secondRepository = new EfLearningRepository(database.CreateContext);
        var assignments = await secondRepository.GetAssignmentsByEmployeeIdAsync(employeeId);

        Assert.That(assignments, Has.Count.EqualTo(1));
        Assert.That(assignments[0].CourseId, Is.EqualTo(course.Id));
        Assert.That(assignments[0].ProgressPercent, Is.EqualTo(40));
    }

    [Test]
    public async Task EfMotivationBonusRepository_FindExistingAsync_FindsSavedBonusAndIgnoresCancelled()
    {
        using var database = new SqliteTestDatabase();

        var repository = new EfMotivationBonusRepository(database.CreateContext);

        var employeeId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var periodStart = new DateTime(2026, 6, 1);
        var periodEnd = new DateTime(2026, 6, 30);

        var bonus = new MotivationBonus
        {
            EmployeeId = employeeId,
            ProgramId = programId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            EfficiencyPercent = 95,
            BaseAmount = 10_000m,
            CalculatedAmount = 9_500m,
            FinalAmount = 9_500m,
            Status = MotivationBonusStatus.PendingApproval,
            Comment = "Тестовый бонус",
            CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.Now
        };

        await repository.AddAsync(bonus);

        var existingBonus = await repository.FindExistingAsync(
            employeeId,
            programId,
            periodStart,
            periodEnd);

        Assert.That(existingBonus, Is.Not.Null);
        Assert.That(existingBonus!.FinalAmount, Is.EqualTo(9_500m));

        existingBonus.Status = MotivationBonusStatus.Cancelled;
        await repository.UpdateAsync(existingBonus);

        var afterCancel = await repository.FindExistingAsync(
            employeeId,
            programId,
            periodStart,
            periodEnd);

        Assert.That(afterCancel, Is.Null);
    }

    [Test]
    public async Task EfAuditLogRepository_AddAndQueryByActionType_PersistsAuditEntry()
    {
        using var database = new SqliteTestDatabase();

        var repository = new EfAuditLogRepository(database.CreateContext);

        var entry = new AuditLogEntry
        {
            UserId = Guid.NewGuid(),
            UserLogin = "admin",
            UserRole = UserRole.Administrator,
            ActionType = AuditActionType.UserLoginSuccess,
            IsSuccess = true,
            Message = "Тестовый вход",
            Details = "Интеграционный тест SQLite",
            CreatedAt = DateTime.Now
        };

        await repository.AddAsync(entry);

        var secondRepository = new EfAuditLogRepository(database.CreateContext);
        var entries = await secondRepository.GetByActionTypeAsync(AuditActionType.UserLoginSuccess);

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].UserLogin, Is.EqualTo("admin"));
        Assert.That(entries[0].Message, Is.EqualTo("Тестовый вход"));
    }

    [Test]
    public async Task EfUserRepository_AddDuplicateLogin_ThrowsDbUpdateException()
    {
        using var database = new SqliteTestDatabase();

        var repository = new EfUserRepository(database.CreateContext);

        var firstUser = new AppUser
        {
            Login = "duplicate-login",
            Email = "first@mbappe.local",
            FullName = "First User",
            PasswordHash = [1],
            PasswordSalt = [2],
            Role = UserRole.Employee,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var secondUser = new AppUser
        {
            Login = "duplicate-login",
            Email = "second@mbappe.local",
            FullName = "Second User",
            PasswordHash = [1],
            PasswordSalt = [2],
            Role = UserRole.Employee,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await repository.AddAsync(firstUser);

        Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await repository.AddAsync(secondUser);
        });
    }
}