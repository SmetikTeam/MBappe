using MBappe.Common;
using MBappe.Models;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests.Services;

[TestFixture]
public class KpiServiceTests
{
    [Test]
    public async Task CreateKpiAsync_AsManagerForDirectReport_CreatesKpiAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.ManagerUser);

        var result = await fixture.Services.KpiService.CreateKpiAsync(new CreateKpiRequest
        {
            EmployeeId = fixture.EmployeeProfile.Id,
            Title = " Sprint delivery ",
            Description = " Deliver planned scope ",
            TargetValue = 100,
            ActualValue = 100,
            Unit = " points ",
            WeightPercent = 80,
            PeriodStart = DateTime.Today,
            PeriodEnd = DateTime.Today.AddDays(30)
        });

        var storedKpis = await fixture.Services.KpiRepository.GetByEmployeeIdAsync(fixture.EmployeeProfile.Id);
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.KpiCreated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kpi, Is.Not.Null);
            Assert.That(storedKpis, Has.One.SameAs(result.Kpi));
            Assert.That(result.Kpi!.Title, Is.EqualTo("Sprint delivery"));
            Assert.That(result.Kpi.Description, Is.EqualTo("Deliver planned scope"));
            Assert.That(result.Kpi.Unit, Is.EqualTo("points"));
            Assert.That(result.Kpi.Status, Is.EqualTo(KpiStatus.Completed));
            Assert.That(result.Kpi.CreatedByUserId, Is.EqualTo(fixture.ManagerUser.Id));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "manager"
                && entry.Details != null
                && entry.Details.Contains("Sprint delivery")));
        });
    }

    [Test]
    public async Task CreateKpiAsync_AsEmployee_ReturnsAccessDenied()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.KpiService.CreateKpiAsync(new CreateKpiRequest
        {
            EmployeeId = fixture.EmployeeProfile.Id,
            Title = "Own KPI",
            Description = "Attempt to create own KPI",
            TargetValue = 10,
            ActualValue = 0,
            Unit = "items",
            WeightPercent = 50,
            PeriodStart = DateTime.Today,
            PeriodEnd = DateTime.Today.AddDays(30)
        });

        var kpis = await fixture.Services.KpiRepository.GetAllAsync();
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для создания KPI"));
            Assert.That(kpis, Is.Empty);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "employee"
                && entry.Details == "Попытка создать KPI без прав"));
        });
    }

    [Test]
    public async Task GetVisibleKpisAsync_ReturnsRoleScopedKpis()
    {
        var adminFixture = await CreateFixtureAsync();
        var managerKpi = await AddKpiAsync(adminFixture, adminFixture.ManagerProfile, "Manager KPI");
        var employeeKpi = await AddKpiAsync(adminFixture, adminFixture.EmployeeProfile, "Employee KPI");
        var otherKpi = await AddKpiAsync(adminFixture, adminFixture.OtherEmployeeProfile, "Other KPI");

        adminFixture.Services.SessionService.StartSession(adminFixture.AdminUser);
        var adminResult = await adminFixture.Services.KpiService.GetVisibleKpisAsync();

        adminFixture.Services.SessionService.StartSession(adminFixture.ManagerUser);
        var managerResult = await adminFixture.Services.KpiService.GetVisibleKpisAsync();

        adminFixture.Services.SessionService.StartSession(adminFixture.EmployeeUser);
        var employeeResult = await adminFixture.Services.KpiService.GetVisibleKpisAsync();

        Assert.Multiple(() =>
        {
            Assert.That(adminResult.Success, Is.True);
            Assert.That(adminResult.Kpis, Is.EquivalentTo(new[] { managerKpi, employeeKpi, otherKpi }));

            Assert.That(managerResult.Success, Is.True);
            Assert.That(managerResult.Kpis, Is.EquivalentTo(new[] { managerKpi, employeeKpi }));

            Assert.That(employeeResult.Success, Is.True);
            Assert.That(employeeResult.Kpis, Is.EquivalentTo(new[] { employeeKpi }));
        });
    }

    [Test]
    public async Task UpdateKpiProgressAsync_CompletesKpiAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        var kpi = await AddKpiAsync(
            fixture,
            fixture.EmployeeProfile,
            "Progress KPI",
            targetValue: 100,
            actualValue: 20,
            status: KpiStatus.InProgress);
        fixture.Services.SessionService.StartSession(fixture.ManagerUser);

        var result = await fixture.Services.KpiService.UpdateKpiProgressAsync(new UpdateKpiProgressRequest
        {
            KpiId = kpi.Id,
            ActualValue = 110
        });

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.KpiProgressUpdated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kpi, Is.SameAs(kpi));
            Assert.That(kpi.ActualValue, Is.EqualTo(110));
            Assert.That(kpi.Status, Is.EqualTo(KpiStatus.Completed));
            Assert.That(kpi.CompletedAt, Is.Not.Null);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "manager"
                && entry.Details != null
                && entry.Details.Contains("новый факт: 110")));
        });
    }

    [Test]
    public async Task CancelKpiAsync_AsEmployee_ReturnsAccessDeniedAndKeepsStatus()
    {
        var fixture = await CreateFixtureAsync();
        var kpi = await AddKpiAsync(fixture, fixture.EmployeeProfile, "Employee KPI");
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.KpiService.CancelKpiAsync(kpi.Id);

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для отмены KPI"));
            Assert.That(kpi.Status, Is.EqualTo(KpiStatus.InProgress));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "employee"
                && entry.Details == "Попытка отменить KPI без прав"));
        });
    }

    [Test]
    public async Task GetEmployeeEfficiencyAsync_CalculatesWeightedCappedEfficiencyAndExcludesCancelledKpis()
    {
        var fixture = await CreateFixtureAsync();
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);
        await AddKpiAsync(
            fixture,
            fixture.EmployeeProfile,
            "Overfulfilled KPI",
            targetValue: 100,
            actualValue: 150,
            weightPercent: 60,
            periodStart: periodStart,
            periodEnd: periodEnd);
        await AddKpiAsync(
            fixture,
            fixture.EmployeeProfile,
            "Partial KPI",
            targetValue: 100,
            actualValue: 50,
            weightPercent: 40,
            periodStart: periodStart,
            periodEnd: periodEnd);
        await AddKpiAsync(
            fixture,
            fixture.EmployeeProfile,
            "Cancelled KPI",
            targetValue: 100,
            actualValue: 100,
            weightPercent: 100,
            periodStart: periodStart,
            periodEnd: periodEnd,
            status: KpiStatus.Cancelled);
        await AddKpiAsync(
            fixture,
            fixture.EmployeeProfile,
            "Out of period KPI",
            targetValue: 100,
            actualValue: 100,
            weightPercent: 100,
            periodStart: new DateTime(2026, 2, 1),
            periodEnd: new DateTime(2026, 2, 28));
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.KpiService.GetEmployeeEfficiencyAsync(
            fixture.EmployeeProfile.Id,
            periodStart,
            periodEnd);

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.KpiEfficiencyCalculated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Efficiency, Is.Not.Null);
            Assert.That(result.Efficiency!.KpiCount, Is.EqualTo(2));
            Assert.That(result.Efficiency.TotalWeight, Is.EqualTo(100));
            Assert.That(result.Efficiency.EfficiencyPercent, Is.EqualTo(92));
            Assert.That(result.Efficiency.Kpis.Select(kpi => kpi.Title), Is.EquivalentTo(new[]
            {
                "Overfulfilled KPI",
                "Partial KPI"
            }));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Details != null
                && entry.Details.Contains("эффективность: 92%")));
        });
    }

    [Test]
    public async Task GetEmployeeEfficiencyAsync_WhenEmployeeHasNoPeriodKpis_ReturnsZeroEfficiency()
    {
        var fixture = await CreateFixtureAsync();
        await AddKpiAsync(
            fixture,
            fixture.EmployeeProfile,
            "Future KPI",
            periodStart: new DateTime(2026, 3, 1),
            periodEnd: new DateTime(2026, 3, 31));
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.KpiService.GetEmployeeEfficiencyAsync(
            fixture.EmployeeProfile.Id,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 31));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("За выбранный период KPI не найдены"));
            Assert.That(result.Efficiency, Is.Not.Null);
            Assert.That(result.Efficiency!.KpiCount, Is.EqualTo(0));
            Assert.That(result.Efficiency.EfficiencyPercent, Is.EqualTo(0));
        });
    }

    private static async Task<KpiFixture> CreateFixtureAsync()
    {
        var services = TestServiceFactory.Create();
        var admin = await GetSeedUserAsync(services, "admin");
        var manager = await GetSeedUserAsync(services, "manager");
        var employee = await GetSeedUserAsync(services, "employee");

        var managerProfile = new EmployeeProfile
        {
            UserId = manager.Id,
            PersonnelNumber = "M-001",
            FullName = "Анна Смирнова",
            Position = "Engineering manager",
            Department = "Engineering",
            Status = EmployeeStatus.Active
        };
        var employeeProfile = new EmployeeProfile
        {
            UserId = employee.Id,
            PersonnelNumber = "E-001",
            FullName = "Иван Петров",
            Position = "Developer",
            Department = "Engineering",
            ManagerEmployeeId = managerProfile.Id,
            Status = EmployeeStatus.Active
        };
        var otherEmployeeProfile = new EmployeeProfile
        {
            UserId = Guid.NewGuid(),
            PersonnelNumber = "O-001",
            FullName = "Петр Другой",
            Position = "Analyst",
            Department = "Sales",
            Status = EmployeeStatus.Active
        };

        await services.EmployeeRepository.AddAsync(managerProfile);
        await services.EmployeeRepository.AddAsync(employeeProfile);
        await services.EmployeeRepository.AddAsync(otherEmployeeProfile);

        return new KpiFixture(
            services,
            admin,
            manager,
            employee,
            managerProfile,
            employeeProfile,
            otherEmployeeProfile);
    }

    private static async Task<AppUser> GetSeedUserAsync(TestAppServices services, string login)
    {
        var user = await services.UserRepository.GetByLoginAsync(login);

        Assert.That(user, Is.Not.Null, $"Seed user '{login}' must exist.");

        return user!;
    }

    private static async Task<KpiItem> AddKpiAsync(
        KpiFixture fixture,
        EmployeeProfile employee,
        string title,
        double targetValue = 100,
        double actualValue = 25,
        double weightPercent = 100,
        DateTime? periodStart = null,
        DateTime? periodEnd = null,
        KpiStatus status = KpiStatus.InProgress)
    {
        var kpi = new KpiItem
        {
            EmployeeId = employee.Id,
            Title = title,
            Description = title,
            TargetValue = targetValue,
            ActualValue = actualValue,
            Unit = "items",
            WeightPercent = weightPercent,
            PeriodStart = periodStart ?? DateTime.Today,
            PeriodEnd = periodEnd ?? DateTime.Today.AddDays(30),
            Status = status,
            CreatedByUserId = fixture.AdminUser.Id
        };

        await fixture.Services.KpiRepository.AddAsync(kpi);
        return kpi;
    }

    private sealed record KpiFixture(
        TestAppServices Services,
        AppUser AdminUser,
        AppUser ManagerUser,
        AppUser EmployeeUser,
        EmployeeProfile ManagerProfile,
        EmployeeProfile EmployeeProfile,
        EmployeeProfile OtherEmployeeProfile);
}
