using MBappe.Models;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests.Services;

[TestFixture]
public class AnalyticsServiceTests
{
    private static readonly DateTime PeriodStart = new(2026, 1, 1);
    private static readonly DateTime PeriodEnd = new(2026, 1, 31);

    [Test]
    public async Task GetDashboardReportAsync_ForAdministratorAndHrSpecialist_ReturnsAllEmployees()
    {
        var administrator = await CreateFixtureAsync();
        administrator.Services.SessionService.StartSession(administrator.AdminUser);

        var administratorResult = await administrator.Services.AnalyticsService.GetDashboardReportAsync(PeriodStart, PeriodEnd);

        var hr = await CreateFixtureAsync();
        hr.Services.SessionService.StartSession(hr.HrUser);

        var hrResult = await hr.Services.AnalyticsService.GetDashboardReportAsync(PeriodStart, PeriodEnd);

        Assert.Multiple(() =>
        {
            Assert.That(administratorResult.Success, Is.True);
            Assert.That(administratorResult.Report?.Summary.ScopeTitle, Is.EqualTo("Все сотрудники"));
            Assert.That(administratorResult.Report?.Summary.TotalEmployees, Is.EqualTo(6));
            Assert.That(administratorResult.Report?.EmployeeRows, Has.Count.EqualTo(6));

            Assert.That(hrResult.Success, Is.True);
            Assert.That(hrResult.Report?.Summary.ScopeTitle, Is.EqualTo("Все сотрудники"));
            Assert.That(hrResult.Report?.Summary.TotalEmployees, Is.EqualTo(6));
            Assert.That(hrResult.Report?.EmployeeRows, Has.Count.EqualTo(6));
        });
    }

    [Test]
    public async Task GetDashboardReportAsync_ForManager_ReturnsManagerAndDirectReportsOnly()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.ManagerUser);

        var result = await fixture.Services.AnalyticsService.GetDashboardReportAsync(PeriodStart, PeriodEnd);
        var employeeNames = result.Report?.EmployeeRows
            .Select(row => row.FullName)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Report?.Summary.ScopeTitle, Is.EqualTo("Руководитель и подчиненные"));
            Assert.That(result.Report?.Summary.TotalEmployees, Is.EqualTo(3));
            Assert.That(employeeNames, Is.EquivalentTo(new[]
            {
                "Анна Смирнова",
                "Иван Петров",
                "Сергей Больничный"
            }));
        });
    }

    [Test]
    public async Task GetDashboardReportAsync_ForEmployee_ReturnsPersonalReportOnly()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.AnalyticsService.GetDashboardReportAsync(PeriodStart, PeriodEnd);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Report?.Summary.ScopeTitle, Is.EqualTo("Личная аналитика"));
            Assert.That(result.Report?.Summary.TotalEmployees, Is.EqualTo(1));
            Assert.That(result.Report?.EmployeeRows.Single().FullName, Is.EqualTo("Иван Петров"));
        });
    }

    [Test]
    public async Task GetDashboardReportAsync_AggregatesPeriodDataAndWritesAuditEntry()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.AnalyticsService.GetDashboardReportAsync(PeriodStart, PeriodEnd);
        var summary = result.Report!.Summary;
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.AnalyticsReportGenerated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);

            Assert.That(summary.PeriodStart, Is.EqualTo(PeriodStart));
            Assert.That(summary.PeriodEnd, Is.EqualTo(PeriodEnd));
            Assert.That(summary.TotalEmployees, Is.EqualTo(6));
            Assert.That(summary.ActiveEmployees, Is.EqualTo(3));
            Assert.That(summary.DismissedEmployees, Is.EqualTo(1));
            Assert.That(summary.OnVacationEmployees, Is.EqualTo(1));
            Assert.That(summary.SickLeaveEmployees, Is.EqualTo(1));
            Assert.That(summary.OnVacationOrSickLeaveEmployees, Is.EqualTo(2));
            Assert.That(summary.DepartmentCount, Is.EqualTo(4));

            Assert.That(summary.TotalKpis, Is.EqualTo(4));
            Assert.That(summary.CompletedKpis, Is.EqualTo(1));
            Assert.That(summary.InProgressKpis, Is.EqualTo(1));
            Assert.That(summary.OverdueKpis, Is.EqualTo(1));
            Assert.That(summary.CancelledKpis, Is.EqualTo(1));
            Assert.That(summary.AverageKpiPercent, Is.EqualTo(60));

            Assert.That(summary.TotalLearningAssignments, Is.EqualTo(5));
            Assert.That(summary.CompletedLearningAssignments, Is.EqualTo(1));
            Assert.That(summary.InProgressLearningAssignments, Is.EqualTo(3));
            Assert.That(summary.CancelledLearningAssignments, Is.EqualTo(1));
            Assert.That(summary.AverageLearningProgressPercent, Is.EqualTo(45));

            Assert.That(summary.TotalBonuses, Is.EqualTo(5));
            Assert.That(summary.PendingBonuses, Is.EqualTo(1));
            Assert.That(summary.ApprovedBonuses, Is.EqualTo(1));
            Assert.That(summary.RejectedBonuses, Is.EqualTo(1));
            Assert.That(summary.PaidBonuses, Is.EqualTo(2));
            Assert.That(summary.PayableBonusAmount, Is.EqualTo(300m));
            Assert.That(summary.PaidBonusAmount, Is.EqualTo(350m));

            Assert.That(result.Report.Insights, Has.Count.EqualTo(6));
            Assert.That(result.Report.Insights[0], Does.Contain("01.01.2026"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Details != null
                && entry.Details.Contains("01.01.2026-31.01.2026")));
        });
    }

    [Test]
    public async Task GetDashboardReportAsync_BuildsEmployeeRowsAndProblemFlags()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.AnalyticsService.GetDashboardReportAsync(PeriodStart, PeriodEnd);

        var employeeRow = result.Report!.EmployeeRows.Single(row => row.FullName == "Иван Петров");
        var managerRow = result.Report.EmployeeRows.Single(row => row.FullName == "Анна Смирнова");
        var adminRow = result.Report.EmployeeRows.Single(row => row.FullName == "Администратор");

        Assert.Multiple(() =>
        {
            Assert.That(employeeRow.Department, Is.EqualTo("Engineering"));
            Assert.That(employeeRow.Position, Is.EqualTo("Developer"));
            Assert.That(employeeRow.Status, Is.EqualTo(EmployeeStatus.Active));
            Assert.That(employeeRow.TotalKpis, Is.EqualTo(3));
            Assert.That(employeeRow.OverdueKpis, Is.EqualTo(1));
            Assert.That(employeeRow.AverageKpiPercent, Is.EqualTo(50));
            Assert.That(employeeRow.TotalLearningAssignments, Is.EqualTo(3));
            Assert.That(employeeRow.CompletedLearningAssignments, Is.EqualTo(1));
            Assert.That(employeeRow.LearningProgressPercent, Is.EqualTo(60));
            Assert.That(employeeRow.PayableBonusAmount, Is.EqualTo(300m));
            Assert.That(employeeRow.PaidBonusAmount, Is.EqualTo(300m));
            Assert.That(employeeRow.ProblemFlags, Does.Contain("Низкий KPI"));
            Assert.That(employeeRow.ProblemFlags, Does.Contain("Есть просроченные KPI"));
            Assert.That(employeeRow.ProblemFlags, Does.Contain("Есть бонусы на утверждении"));
            Assert.That(employeeRow.ProblemFlags, Does.Not.Contain("Без замечаний"));

            Assert.That(managerRow.AverageKpiPercent, Is.EqualTo(80));
            Assert.That(managerRow.LearningProgressPercent, Is.EqualTo(40));
            Assert.That(managerRow.ProblemFlags, Is.EquivalentTo(new[] { "Низкий прогресс обучения" }));

            Assert.That(adminRow.ProblemFlags, Is.EquivalentTo(new[] { "Без замечаний" }));
        });
    }

    [Test]
    public async Task GetDashboardReportAsync_WhenPeriodEndBeforeStart_ReturnsFailure()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.AnalyticsService.GetDashboardReportAsync(PeriodEnd, PeriodStart);
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.AnalyticsReportGenerated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Report, Is.Null);
            Assert.That(result.Message, Does.Contain("Дата окончания периода"));
            Assert.That(auditEntries, Is.Empty);
        });
    }

    [Test]
    public async Task GetDashboardReportAsync_WhenUserIsNotAuthenticated_ReturnsFailureAndWritesAccessDeniedAudit()
    {
        var services = TestServiceFactory.Create();

        var result = await services.AnalyticsService.GetDashboardReportAsync(PeriodStart, PeriodEnd);
        var auditEntries = await services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Report, Is.Null);
            Assert.That(result.Message, Is.EqualTo("Пользователь не авторизован"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess && entry.Message == "Отказано в доступе"));
        });
    }

    [Test]
    public async Task GetDashboardReportAsync_WhenEmployeeProfileIsMissing_ReturnsFailure()
    {
        var services = TestServiceFactory.Create();
        var employeeUser = await GetSeedUserAsync(services, "employee");
        services.SessionService.StartSession(employeeUser);

        var result = await services.AnalyticsService.GetDashboardReportAsync(PeriodStart, PeriodEnd);
        var auditEntries = await services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.AnalyticsReportGenerated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Report, Is.Null);
            Assert.That(result.Message, Is.EqualTo("Для текущей учетной записи не создан профиль сотрудника"));
            Assert.That(auditEntries, Is.Empty);
        });
    }

    private static async Task<AnalyticsFixture> CreateFixtureAsync()
    {
        var services = TestServiceFactory.Create();

        var admin = await GetSeedUserAsync(services, "admin");
        var hr = await GetSeedUserAsync(services, "hr");
        var manager = await GetSeedUserAsync(services, "manager");
        var employee = await GetSeedUserAsync(services, "employee");

        var adminProfile = new EmployeeProfile
        {
            UserId = admin.Id,
            PersonnelNumber = "A-001",
            FullName = "Администратор",
            Department = "IT",
            Position = "Administrator",
            Status = EmployeeStatus.Active
        };
        var hrProfile = new EmployeeProfile
        {
            UserId = hr.Id,
            PersonnelNumber = "H-001",
            FullName = "Мария HR",
            Department = "HR",
            Position = "HR specialist",
            Status = EmployeeStatus.OnVacation
        };
        var managerProfile = new EmployeeProfile
        {
            UserId = manager.Id,
            PersonnelNumber = "M-001",
            FullName = "Анна Смирнова",
            Department = "Engineering",
            Position = "Engineering manager",
            Status = EmployeeStatus.Active
        };
        var employeeProfile = new EmployeeProfile
        {
            UserId = employee.Id,
            PersonnelNumber = "E-001",
            FullName = "Иван Петров",
            Department = "Engineering",
            Position = "Developer",
            ManagerEmployeeId = managerProfile.Id,
            Status = EmployeeStatus.Active
        };
        var dismissedProfile = new EmployeeProfile
        {
            UserId = Guid.NewGuid(),
            PersonnelNumber = "D-001",
            FullName = "Павел Уволенный",
            Department = "Sales",
            Position = "Sales manager",
            Status = EmployeeStatus.Dismissed
        };
        var sickProfile = new EmployeeProfile
        {
            UserId = Guid.NewGuid(),
            PersonnelNumber = "S-001",
            FullName = "Сергей Больничный",
            Department = "Engineering",
            Position = "QA engineer",
            ManagerEmployeeId = managerProfile.Id,
            Status = EmployeeStatus.SickLeave
        };

        foreach (var profile in new[]
        {
            adminProfile,
            hrProfile,
            managerProfile,
            employeeProfile,
            dismissedProfile,
            sickProfile
        })
        {
            await services.EmployeeRepository.AddAsync(profile);
        }

        await SeedKpisAsync(services, managerProfile, employeeProfile);
        await SeedLearningAssignmentsAsync(services, managerProfile, employeeProfile, sickProfile);
        await SeedBonusesAsync(services, managerProfile, employeeProfile);

        return new AnalyticsFixture(
            services,
            admin,
            hr,
            manager,
            employee,
            adminProfile,
            hrProfile,
            managerProfile,
            employeeProfile,
            dismissedProfile,
            sickProfile);
    }

    private static async Task<AppUser> GetSeedUserAsync(TestAppServices services, string login)
    {
        var user = await services.UserRepository.GetByLoginAsync(login);

        Assert.That(user, Is.Not.Null, $"Seed user '{login}' must exist.");

        return user!;
    }

    private static async Task SeedKpisAsync(
        TestAppServices services,
        EmployeeProfile managerProfile,
        EmployeeProfile employeeProfile)
    {
        await services.KpiRepository.AddAsync(new KpiItem
        {
            EmployeeId = employeeProfile.Id,
            Title = "Feature delivery",
            TargetValue = 100,
            ActualValue = 60,
            PeriodStart = new DateTime(2026, 1, 5),
            PeriodEnd = new DateTime(2026, 1, 20),
            Status = KpiStatus.Completed
        });
        await services.KpiRepository.AddAsync(new KpiItem
        {
            EmployeeId = employeeProfile.Id,
            Title = "Bug fixing",
            TargetValue = 100,
            ActualValue = 40,
            PeriodStart = new DateTime(2026, 1, 15),
            PeriodEnd = new DateTime(2026, 2, 15),
            Status = KpiStatus.Overdue
        });
        await services.KpiRepository.AddAsync(new KpiItem
        {
            EmployeeId = employeeProfile.Id,
            Title = "Cancelled target",
            TargetValue = 100,
            ActualValue = 90,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            Status = KpiStatus.Cancelled
        });
        await services.KpiRepository.AddAsync(new KpiItem
        {
            EmployeeId = employeeProfile.Id,
            Title = "Future target",
            TargetValue = 100,
            ActualValue = 100,
            PeriodStart = new DateTime(2026, 2, 1),
            PeriodEnd = new DateTime(2026, 2, 28),
            Status = KpiStatus.Completed
        });
        await services.KpiRepository.AddAsync(new KpiItem
        {
            EmployeeId = managerProfile.Id,
            Title = "Team planning",
            TargetValue = 100,
            ActualValue = 80,
            PeriodStart = new DateTime(2025, 12, 15),
            PeriodEnd = new DateTime(2026, 1, 15),
            Status = KpiStatus.InProgress
        });
    }

    private static async Task SeedLearningAssignmentsAsync(
        TestAppServices services,
        EmployeeProfile managerProfile,
        EmployeeProfile employeeProfile,
        EmployeeProfile sickProfile)
    {
        await AddAssignmentAsync(
            services,
            employeeProfile,
            LearningAssignmentStatus.Completed,
            100,
            new DateTime(2026, 1, 2),
            completedAt: new DateTime(2026, 1, 10));
        await AddAssignmentAsync(
            services,
            employeeProfile,
            LearningAssignmentStatus.InProgress,
            20,
            new DateTime(2026, 1, 5),
            dueDate: new DateTime(2026, 1, 31));
        await AddAssignmentAsync(
            services,
            employeeProfile,
            LearningAssignmentStatus.Cancelled,
            10,
            new DateTime(2026, 1, 5),
            dueDate: new DateTime(2026, 1, 31));
        await AddAssignmentAsync(
            services,
            employeeProfile,
            LearningAssignmentStatus.Completed,
            100,
            new DateTime(2026, 2, 1),
            completedAt: new DateTime(2026, 2, 10));
        await AddAssignmentAsync(
            services,
            managerProfile,
            LearningAssignmentStatus.Assigned,
            40,
            new DateTime(2025, 12, 25),
            dueDate: new DateTime(2026, 1, 5));
        await AddAssignmentAsync(
            services,
            sickProfile,
            LearningAssignmentStatus.InProgress,
            20,
            new DateTime(2026, 1, 12),
            dueDate: new DateTime(2026, 1, 25));
    }

    private static Task AddAssignmentAsync(
        TestAppServices services,
        EmployeeProfile employee,
        LearningAssignmentStatus status,
        double progressPercent,
        DateTime assignedAt,
        DateTime? dueDate = null,
        DateTime? completedAt = null)
    {
        return services.LearningRepository.AddAssignmentAsync(new LearningAssignment
        {
            CourseId = Guid.NewGuid(),
            EmployeeId = employee.Id,
            AssignedByUserId = Guid.NewGuid(),
            AssignedAt = assignedAt,
            DueDate = dueDate,
            CompletedAt = completedAt,
            ProgressPercent = progressPercent,
            Status = status
        });
    }

    private static async Task SeedBonusesAsync(
        TestAppServices services,
        EmployeeProfile managerProfile,
        EmployeeProfile employeeProfile)
    {
        await AddBonusAsync(
            services,
            employeeProfile,
            MotivationBonusStatus.PendingApproval,
            100,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 31));
        await AddBonusAsync(
            services,
            employeeProfile,
            MotivationBonusStatus.Approved,
            200,
            new DateTime(2026, 1, 15),
            new DateTime(2026, 2, 15));
        await AddBonusAsync(
            services,
            employeeProfile,
            MotivationBonusStatus.Paid,
            300,
            new DateTime(2025, 12, 15),
            new DateTime(2026, 1, 10));
        await AddBonusAsync(
            services,
            employeeProfile,
            MotivationBonusStatus.Rejected,
            400,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 31));
        await AddBonusAsync(
            services,
            employeeProfile,
            MotivationBonusStatus.Approved,
            999,
            new DateTime(2026, 2, 1),
            new DateTime(2026, 2, 28));
        await AddBonusAsync(
            services,
            managerProfile,
            MotivationBonusStatus.Paid,
            50,
            new DateTime(2026, 1, 5),
            new DateTime(2026, 1, 20));
    }

    private static Task AddBonusAsync(
        TestAppServices services,
        EmployeeProfile employee,
        MotivationBonusStatus status,
        decimal finalAmount,
        DateTime periodStart,
        DateTime periodEnd)
    {
        return services.MotivationBonusRepository.AddAsync(new MotivationBonus
        {
            EmployeeId = employee.Id,
            ProgramId = Guid.NewGuid(),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            BaseAmount = finalAmount,
            CalculatedAmount = finalAmount,
            FinalAmount = finalAmount,
            Status = status,
            CreatedByUserId = Guid.NewGuid()
        });
    }

    private sealed record AnalyticsFixture(
        TestAppServices Services,
        AppUser AdminUser,
        AppUser HrUser,
        AppUser ManagerUser,
        AppUser EmployeeUser,
        EmployeeProfile AdminProfile,
        EmployeeProfile HrProfile,
        EmployeeProfile ManagerProfile,
        EmployeeProfile EmployeeProfile,
        EmployeeProfile DismissedProfile,
        EmployeeProfile SickProfile);
}
