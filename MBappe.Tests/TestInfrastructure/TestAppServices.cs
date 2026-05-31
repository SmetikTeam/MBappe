using MBappe.Repositories;
using MBappe.Services;

namespace MBappe.Tests.TestInfrastructure;

public sealed class TestAppServices
{
    public InMemoryUserRepository UserRepository { get; } = new();

    public InMemoryEmployeeRepository EmployeeRepository { get; } = new();

    public InMemoryKpiRepository KpiRepository { get; } = new();

    public InMemoryLearningRepository LearningRepository { get; } = new();

    public InMemoryMotivationProgramRepository MotivationProgramRepository { get; } = new();

    public InMemoryMotivationBonusRepository MotivationBonusRepository { get; } = new();

    public InMemoryAuditLogRepository AuditLogRepository { get; } = new();

    public PasswordHasher PasswordHasher { get; } = new();

    public SessionService SessionService { get; } = new();

    public AuditLogService AuditLogService { get; }

    public AuthService AuthService { get; }

    public UserManagementService UserManagementService { get; }

    public EmployeeService EmployeeService { get; }

    public KpiService KpiService { get; }

    public LearningService LearningService { get; }

    public MotivationService MotivationService { get; }

    public AnalyticsService AnalyticsService { get; }

    public TestAppServices()
    {
        AuditLogService = new AuditLogService(AuditLogRepository, SessionService);

        AuthService = new AuthService(
            UserRepository,
            PasswordHasher,
            SessionService,
            AuditLogService);

        UserManagementService = new UserManagementService(
            UserRepository,
            PasswordHasher,
            SessionService,
            AuditLogService);

        EmployeeService = new EmployeeService(
            EmployeeRepository,
            UserRepository,
            SessionService,
            AuditLogService);

        KpiService = new KpiService(
            KpiRepository,
            EmployeeRepository,
            SessionService,
            AuditLogService);

        LearningService = new LearningService(
            LearningRepository,
            EmployeeRepository,
            SessionService,
            AuditLogService);

        MotivationService = new MotivationService(
            MotivationProgramRepository,
            MotivationBonusRepository,
            EmployeeRepository,
            KpiService,
            SessionService,
            AuditLogService);

        AnalyticsService = new AnalyticsService(
            EmployeeRepository,
            KpiRepository,
            LearningRepository,
            MotivationBonusRepository,
            SessionService,
            AuditLogService);
    }
}
