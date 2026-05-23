using MBappe.Repositories;

namespace MBappe.Services;

public static class AppServices
{
    public static IUserRepository UserRepository { get; } = new InMemoryUserRepository();

    public static PasswordHasher PasswordHasher { get; } = new PasswordHasher();

    public static SessionService SessionService { get; } = new SessionService();

    public static AuthService AuthService { get; } = new AuthService(
        UserRepository,
        PasswordHasher,
        SessionService);
}