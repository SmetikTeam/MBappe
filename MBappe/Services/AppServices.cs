using MBappe.Repositories;

namespace MBappe.Services;

public static class AppServices
{
    public static IUserRepository UserRepository { get; } = new EfUserRepository();

    public static IEmployeeRepository EmployeeRepository { get; } = new EfEmployeeRepository();

    public static IKpiRepository KpiRepository { get; } = new EfKpiRepository();


    public static ILearningRepository LearningRepository { get; } = new EfLearningRepository();

    public static IMotivationProgramRepository MotivationProgramRepository { get; } =
        new EfMotivationProgramRepository();

    public static IMotivationBonusRepository MotivationBonusRepository { get; } =
        new EfMotivationBonusRepository();


    public static IAuditLogRepository AuditLogRepository { get; } = new EfAuditLogRepository();

    public static PasswordHasher PasswordHasher { get; } = new PasswordHasher();

    public static SessionService SessionService { get; } = new SessionService();

    public static AuditLogService AuditLogService { get; } = new AuditLogService(
        AuditLogRepository,
        SessionService);

    public static AuthService AuthService { get; } = new AuthService(
        UserRepository,
        PasswordHasher,
        SessionService,
        AuditLogService);

    public static UserManagementService UserManagementService { get; } = new UserManagementService(
        UserRepository,
        PasswordHasher,
        SessionService,
        AuditLogService);

    public static EmployeeService EmployeeService { get; } = new EmployeeService(
        EmployeeRepository,
        UserRepository,
        SessionService,
        AuditLogService);

    public static KpiService KpiService { get; } = new KpiService(
        KpiRepository,
        EmployeeRepository,
        SessionService,
        AuditLogService);

    public static LearningService LearningService { get; } = new LearningService(
        LearningRepository,
        EmployeeRepository,
        SessionService,
        AuditLogService);

    public static MotivationService MotivationService { get; } = new MotivationService(
        MotivationProgramRepository,
        MotivationBonusRepository,
        EmployeeRepository,
        KpiService,
        SessionService,
        AuditLogService);

    public static AnalyticsService AnalyticsService { get; } = new AnalyticsService(
        EmployeeRepository,
        KpiRepository,
        LearningRepository,
        MotivationBonusRepository,
        SessionService,
        AuditLogService);
}
