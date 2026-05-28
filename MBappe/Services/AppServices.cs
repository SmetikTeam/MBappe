using MBappe.Repositories;

namespace MBappe.Services;

public static class AppServices
{
    public static IUserRepository UserRepository { get; } = new InMemoryUserRepository();

    public static IEmployeeRepository EmployeeRepository { get; } = new InMemoryEmployeeRepository();

    public static IKpiRepository KpiRepository { get; } = new InMemoryKpiRepository();

    public static IMotivationProgramRepository MotivationProgramRepository { get; } =
        new InMemoryMotivationProgramRepository();

    public static IMotivationBonusRepository MotivationBonusRepository { get; } =
        new InMemoryMotivationBonusRepository();

    public static IAuditLogRepository AuditLogRepository { get; } = new InMemoryAuditLogRepository();

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

    public static MotivationService MotivationService { get; } = new MotivationService(
        MotivationProgramRepository,
        MotivationBonusRepository,
        EmployeeRepository,
        KpiService,
        SessionService,
        AuditLogService);
}