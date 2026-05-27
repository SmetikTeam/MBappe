using MBappe.Repositories;

namespace MBappe.Services;

public static class AppServices
{
    public static IUserRepository UserRepository { get; } = new InMemoryUserRepository();

    public static IEmployeeRepository EmployeeRepository { get; } = new InMemoryEmployeeRepository();

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
}
