using MBappe.Common;
using MBappe.Models;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests.Services;

[TestFixture]
public class MotivationServiceTests
{
    private static readonly DateTime PeriodStart = new(2026, 1, 1);
    private static readonly DateTime PeriodEnd = new(2026, 1, 31);

    [Test]
    public async Task CreateProgramAsync_AsHrSpecialist_CreatesActiveProgramAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.MotivationService.CreateProgramAsync(
            new CreateMotivationProgramRequest
            {
                Title = " Quarterly KPI bonus ",
                Description = " Quarterly bonus program ",
                BaseAmount = 25_000m,
                MinEfficiencyPercent = 50,
                MaxEfficiencyPercent = 130
            });

        var programs = await fixture.Services.MotivationProgramRepository.GetAllAsync();
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.MotivationProgramCreated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Program, Is.Not.Null);
            Assert.That(programs, Has.Some.SameAs(result.Program));
            Assert.That(result.Program!.Title, Is.EqualTo("Quarterly KPI bonus"));
            Assert.That(result.Program.Description, Is.EqualTo("Quarterly bonus program"));
            Assert.That(result.Program.BaseAmount, Is.EqualTo(25_000m));
            Assert.That(result.Program.MinEfficiencyPercent, Is.EqualTo(50));
            Assert.That(result.Program.MaxEfficiencyPercent, Is.EqualTo(130));
            Assert.That(result.Program.IsActive, Is.True);
            Assert.That(result.Program.CreatedByUserId, Is.EqualTo(fixture.HrUser.Id));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "hr"
                && entry.Details != null
                && entry.Details.Contains("Quarterly KPI bonus")));
        });
    }

    [Test]
    public async Task CreateProgramAsync_AsManager_ReturnsAccessDenied()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.ManagerUser);

        var result = await fixture.Services.MotivationService.CreateProgramAsync(
            new CreateMotivationProgramRequest
            {
                Title = "Manager program",
                Description = "Manager program",
                BaseAmount = 10_000m
            });

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для создания программы мотивации"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "manager"
                && entry.Details == "Попытка создать мотивационную программу без прав"));
        });
    }

    [Test]
    public async Task UpdateProgramAsync_AsAdministrator_UpdatesFieldsAndActivity()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "Initial program", 10_000m);
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.MotivationService.UpdateProgramAsync(
            new UpdateMotivationProgramRequest
            {
                ProgramId = program.Id,
                Title = " Updated program ",
                Description = " Updated description ",
                BaseAmount = 15_000m,
                MinEfficiencyPercent = 70,
                MaxEfficiencyPercent = 140,
                IsActive = false
            });

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.MotivationProgramUpdated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Program, Is.SameAs(program));
            Assert.That(program.Title, Is.EqualTo("Updated program"));
            Assert.That(program.Description, Is.EqualTo("Updated description"));
            Assert.That(program.BaseAmount, Is.EqualTo(15_000m));
            Assert.That(program.MinEfficiencyPercent, Is.EqualTo(70));
            Assert.That(program.MaxEfficiencyPercent, Is.EqualTo(140));
            Assert.That(program.IsActive, Is.False);
            Assert.That(program.UpdatedAt, Is.Not.Null);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Details != null
                && entry.Details.Contains("активна: False")));
        });
    }

    [Test]
    public async Task CalculateBonusAsync_AsManagerForDirectReport_CreatesPendingBonusFromKpiEfficiency()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "KPI bonus", 1_000m);
        await AddKpiAsync(fixture, fixture.EmployeeProfile, "High KPI", 100, 150, 60);
        await AddKpiAsync(fixture, fixture.EmployeeProfile, "Partial KPI", 100, 50, 40);
        fixture.Services.SessionService.StartSession(fixture.ManagerUser);

        var result = await fixture.Services.MotivationService.CalculateBonusAsync(new CalculateBonusRequest
        {
            EmployeeId = fixture.EmployeeProfile.Id,
            ProgramId = program.Id,
            PeriodStart = PeriodStart,
            PeriodEnd = PeriodEnd
        });

        var storedBonuses = await fixture.Services.MotivationBonusRepository.GetByEmployeeIdAsync(
            fixture.EmployeeProfile.Id);
        var bonusCalculatedAudit = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.BonusCalculated);
        var efficiencyAudit = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.KpiEfficiencyCalculated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bonus, Is.Not.Null);
            Assert.That(storedBonuses, Has.One.SameAs(result.Bonus));
            Assert.That(result.Bonus!.EmployeeId, Is.EqualTo(fixture.EmployeeProfile.Id));
            Assert.That(result.Bonus.ProgramId, Is.EqualTo(program.Id));
            Assert.That(result.Bonus.PeriodStart, Is.EqualTo(PeriodStart));
            Assert.That(result.Bonus.PeriodEnd, Is.EqualTo(PeriodEnd));
            Assert.That(result.Bonus.EfficiencyPercent, Is.EqualTo(92));
            Assert.That(result.Bonus.BaseAmount, Is.EqualTo(1_000m));
            Assert.That(result.Bonus.CalculatedAmount, Is.EqualTo(920m));
            Assert.That(result.Bonus.FinalAmount, Is.EqualTo(920m));
            Assert.That(result.Bonus.Status, Is.EqualTo(MotivationBonusStatus.PendingApproval));
            Assert.That(result.Bonus.Comment, Is.EqualTo("Бонус рассчитан автоматически по KPI."));
            Assert.That(result.Bonus.CreatedByUserId, Is.EqualTo(fixture.ManagerUser.Id));
            Assert.That(bonusCalculatedAudit, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "manager"
                && entry.Details != null
                && entry.Details.Contains("сумма: 920")));
            Assert.That(efficiencyAudit, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "manager"
                && entry.Details != null
                && entry.Details.Contains("эффективность: 92%")));
        });
    }

    [Test]
    public async Task CalculateBonusAsync_WhenEfficiencyBelowMinimum_CreatesZeroBonusWithComment()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "Strict KPI bonus", 1_000m, minEfficiencyPercent: 60);
        await AddKpiAsync(fixture, fixture.EmployeeProfile, "Low KPI", 100, 50, 100);
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.MotivationService.CalculateBonusAsync(new CalculateBonusRequest
        {
            EmployeeId = fixture.EmployeeProfile.Id,
            ProgramId = program.Id,
            PeriodStart = PeriodStart,
            PeriodEnd = PeriodEnd
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bonus, Is.Not.Null);
            Assert.That(result.Bonus!.EfficiencyPercent, Is.EqualTo(50));
            Assert.That(result.Bonus.FinalAmount, Is.EqualTo(0m));
            Assert.That(result.Bonus.Comment, Is.EqualTo("Бонус не начислен из-за эффективности ниже минимального порога."));
        });
    }

    [Test]
    public async Task CalculateBonusAsync_WhenDuplicatePeriodProgramExists_ReturnsFailure()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "KPI bonus", 1_000m);
        await AddKpiAsync(fixture, fixture.EmployeeProfile, "KPI", 100, 100, 100);
        await AddBonusAsync(
            fixture,
            fixture.EmployeeProfile,
            program,
            MotivationBonusStatus.PendingApproval,
            1_000m);
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.MotivationService.CalculateBonusAsync(new CalculateBonusRequest
        {
            EmployeeId = fixture.EmployeeProfile.Id,
            ProgramId = program.Id,
            PeriodStart = PeriodStart,
            PeriodEnd = PeriodEnd
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Бонус за этот период по выбранной программе уже рассчитан"));
            Assert.That(result.Bonus, Is.Null);
        });
    }

    [Test]
    public async Task CalculateBonusAsync_AsEmployee_ReturnsAccessDenied()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "KPI bonus", 1_000m);
        await AddKpiAsync(fixture, fixture.EmployeeProfile, "KPI", 100, 100, 100);
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.MotivationService.CalculateBonusAsync(new CalculateBonusRequest
        {
            EmployeeId = fixture.EmployeeProfile.Id,
            ProgramId = program.Id,
            PeriodStart = PeriodStart,
            PeriodEnd = PeriodEnd
        });

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для расчета бонуса сотрудника"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "employee"
                && entry.Details == "Попытка рассчитать бонус без прав"));
        });
    }

    [Test]
    public async Task GetVisibleBonusesAsync_ReturnsRoleScopedBonuses()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "KPI bonus", 1_000m);
        var managerBonus = await AddBonusAsync(
            fixture,
            fixture.ManagerProfile,
            program,
            MotivationBonusStatus.PendingApproval,
            500m);
        var employeeBonus = await AddBonusAsync(
            fixture,
            fixture.EmployeeProfile,
            program,
            MotivationBonusStatus.Approved,
            700m);
        var otherBonus = await AddBonusAsync(
            fixture,
            fixture.OtherEmployeeProfile,
            program,
            MotivationBonusStatus.Paid,
            900m);

        fixture.Services.SessionService.StartSession(fixture.AdminUser);
        var adminResult = await fixture.Services.MotivationService.GetVisibleBonusesAsync();

        fixture.Services.SessionService.StartSession(fixture.ManagerUser);
        var managerResult = await fixture.Services.MotivationService.GetVisibleBonusesAsync();

        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);
        var employeeResult = await fixture.Services.MotivationService.GetVisibleBonusesAsync();

        Assert.Multiple(() =>
        {
            Assert.That(adminResult.Success, Is.True);
            Assert.That(adminResult.Bonuses, Is.EquivalentTo(new[]
            {
                managerBonus,
                employeeBonus,
                otherBonus
            }));

            Assert.That(managerResult.Success, Is.True);
            Assert.That(managerResult.Bonuses, Is.EquivalentTo(new[] { managerBonus, employeeBonus }));

            Assert.That(employeeResult.Success, Is.True);
            Assert.That(employeeResult.Bonuses, Is.EquivalentTo(new[] { employeeBonus }));
        });
    }

    [Test]
    public async Task ApproveAndPayBonusAsync_AsAdministrator_TransitionsBonusAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "KPI bonus", 1_000m);
        var bonus = await AddBonusAsync(
            fixture,
            fixture.EmployeeProfile,
            program,
            MotivationBonusStatus.PendingApproval,
            800m);
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var approveResult = await fixture.Services.MotivationService.ApproveBonusAsync(bonus.Id);
        var statusAfterApprove = bonus.Status;
        var approvedByUserId = bonus.ApprovedByUserId;
        var approvedAt = bonus.ApprovedAt;

        var payResult = await fixture.Services.MotivationService.MarkBonusAsPaidAsync(bonus.Id);

        var approveAudit = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.BonusApproved);
        var paidAudit = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.BonusPaid);

        Assert.Multiple(() =>
        {
            Assert.That(approveResult.Success, Is.True);
            Assert.That(approveResult.Bonus, Is.SameAs(bonus));
            Assert.That(statusAfterApprove, Is.EqualTo(MotivationBonusStatus.Approved));
            Assert.That(approvedByUserId, Is.EqualTo(fixture.AdminUser.Id));
            Assert.That(approvedAt, Is.Not.Null);

            Assert.That(payResult.Success, Is.True);
            Assert.That(payResult.Bonus, Is.SameAs(bonus));
            Assert.That(bonus.Status, Is.EqualTo(MotivationBonusStatus.Paid));
            Assert.That(bonus.PaidByUserId, Is.EqualTo(fixture.AdminUser.Id));
            Assert.That(bonus.PaidAt, Is.Not.Null);

            Assert.That(approveAudit, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess && entry.UserLogin == "admin"));
            Assert.That(paidAudit, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess && entry.UserLogin == "admin"));
        });
    }

    [Test]
    public async Task RejectBonusAsync_AsHrSpecialist_RejectsPendingBonusWithTrimmedComment()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "KPI bonus", 1_000m);
        var bonus = await AddBonusAsync(
            fixture,
            fixture.EmployeeProfile,
            program,
            MotivationBonusStatus.PendingApproval,
            800m);
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.MotivationService.RejectBonusAsync(bonus.Id, " Needs review ");

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.BonusRejected);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bonus, Is.SameAs(bonus));
            Assert.That(bonus.Status, Is.EqualTo(MotivationBonusStatus.Rejected));
            Assert.That(bonus.Comment, Is.EqualTo("Needs review"));
            Assert.That(bonus.RejectedByUserId, Is.EqualTo(fixture.HrUser.Id));
            Assert.That(bonus.RejectedAt, Is.Not.Null);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "hr"
                && entry.Details != null
                && entry.Details.Contains("Needs review")));
        });
    }

    [Test]
    public async Task CancelBonusAsync_AsHrSpecialist_CancelsApprovedBonus()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "KPI bonus", 1_000m);
        var bonus = await AddBonusAsync(
            fixture,
            fixture.EmployeeProfile,
            program,
            MotivationBonusStatus.Approved,
            800m);
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.MotivationService.CancelBonusAsync(bonus.Id);

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.BonusCancelled);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bonus, Is.SameAs(bonus));
            Assert.That(bonus.Status, Is.EqualTo(MotivationBonusStatus.Cancelled));
            Assert.That(bonus.Comment, Is.EqualTo("Бонус отменен."));
            Assert.That(bonus.CancelledByUserId, Is.EqualTo(fixture.HrUser.Id));
            Assert.That(bonus.CancelledAt, Is.Not.Null);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess && entry.UserLogin == "hr"));
        });
    }

    [Test]
    public async Task CancelBonusAsync_WhenBonusIsPaid_ReturnsFailure()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "KPI bonus", 1_000m);
        var bonus = await AddBonusAsync(
            fixture,
            fixture.EmployeeProfile,
            program,
            MotivationBonusStatus.Paid,
            800m);
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.MotivationService.CancelBonusAsync(bonus.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Нельзя отменить уже выплаченный бонус"));
            Assert.That(bonus.Status, Is.EqualTo(MotivationBonusStatus.Paid));
        });
    }

    [Test]
    public async Task ApproveBonusAsync_AsManager_ReturnsAccessDeniedAndKeepsStatus()
    {
        var fixture = await CreateFixtureAsync();
        var program = await AddProgramAsync(fixture, "KPI bonus", 1_000m);
        var bonus = await AddBonusAsync(
            fixture,
            fixture.EmployeeProfile,
            program,
            MotivationBonusStatus.PendingApproval,
            800m);
        fixture.Services.SessionService.StartSession(fixture.ManagerUser);

        var result = await fixture.Services.MotivationService.ApproveBonusAsync(bonus.Id);

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для утверждения бонуса"));
            Assert.That(bonus.Status, Is.EqualTo(MotivationBonusStatus.PendingApproval));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "manager"
                && entry.Details == "Попытка утвердить бонус без прав"));
        });
    }

    private static async Task<MotivationFixture> CreateFixtureAsync()
    {
        var services = TestServiceFactory.Create();
        var admin = await GetSeedUserAsync(services, "admin");
        var hr = await GetSeedUserAsync(services, "hr");
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

        return new MotivationFixture(
            services,
            admin,
            hr,
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

    private static async Task<MotivationProgram> AddProgramAsync(
        MotivationFixture fixture,
        string title,
        decimal baseAmount,
        double minEfficiencyPercent = 60,
        double maxEfficiencyPercent = 120,
        bool isActive = true)
    {
        var program = new MotivationProgram
        {
            Title = title,
            Description = title,
            BaseAmount = baseAmount,
            MinEfficiencyPercent = minEfficiencyPercent,
            MaxEfficiencyPercent = maxEfficiencyPercent,
            IsActive = isActive,
            CreatedByUserId = fixture.AdminUser.Id
        };

        await fixture.Services.MotivationProgramRepository.AddAsync(program);
        return program;
    }

    private static async Task<KpiItem> AddKpiAsync(
        MotivationFixture fixture,
        EmployeeProfile employee,
        string title,
        double targetValue,
        double actualValue,
        double weightPercent)
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
            PeriodStart = PeriodStart,
            PeriodEnd = PeriodEnd,
            Status = KpiStatus.InProgress,
            CreatedByUserId = fixture.AdminUser.Id
        };

        await fixture.Services.KpiRepository.AddAsync(kpi);
        return kpi;
    }

    private static async Task<MotivationBonus> AddBonusAsync(
        MotivationFixture fixture,
        EmployeeProfile employee,
        MotivationProgram program,
        MotivationBonusStatus status,
        decimal finalAmount)
    {
        var bonus = new MotivationBonus
        {
            EmployeeId = employee.Id,
            ProgramId = program.Id,
            PeriodStart = PeriodStart,
            PeriodEnd = PeriodEnd,
            EfficiencyPercent = 100,
            BaseAmount = program.BaseAmount,
            CalculatedAmount = finalAmount,
            FinalAmount = finalAmount,
            Status = status,
            Comment = "Seed bonus",
            CreatedByUserId = fixture.AdminUser.Id
        };

        await fixture.Services.MotivationBonusRepository.AddAsync(bonus);
        return bonus;
    }

    private sealed record MotivationFixture(
        TestAppServices Services,
        AppUser AdminUser,
        AppUser HrUser,
        AppUser ManagerUser,
        AppUser EmployeeUser,
        EmployeeProfile ManagerProfile,
        EmployeeProfile EmployeeProfile,
        EmployeeProfile OtherEmployeeProfile);
}
