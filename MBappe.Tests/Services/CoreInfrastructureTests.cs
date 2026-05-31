using MBappe.Models;
using MBappe.Services;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests.Services;

[TestFixture]
public class CoreInfrastructureTests
{
    [Test]
    public void PasswordHasher_GeneratesDifferentSaltsAndVerifiesOnlyMatchingPassword()
    {
        var hasher = new PasswordHasher();

        var firstSalt = hasher.GenerateSalt();
        var secondSalt = hasher.GenerateSalt();
        var hash = hasher.HashPassword("correct-password", firstSalt);

        Assert.Multiple(() =>
        {
            Assert.That(firstSalt, Is.Not.Empty);
            Assert.That(secondSalt, Is.Not.Empty);
            Assert.That(firstSalt, Is.Not.EqualTo(secondSalt));
            Assert.That(hash, Is.Not.Empty);
            Assert.That(hasher.VerifyPassword("correct-password", firstSalt, hash), Is.True);
            Assert.That(hasher.VerifyPassword("wrong-password", firstSalt, hash), Is.False);
            Assert.That(hasher.HashPassword("correct-password", secondSalt), Is.Not.EqualTo(hash));
        });
    }

    [Test]
    public void SessionService_StartEndAndRoleChecksReflectCurrentUser()
    {
        var sessionService = new SessionService();
        var user = new AppUser
        {
            Login = "manager",
            FullName = "Manager User",
            Role = UserRole.Manager
        };

        sessionService.StartSession(user);
        var hasManagerRole = sessionService.HasRole(UserRole.Manager);
        var hasAnyManagementRole = sessionService.HasAnyRole(UserRole.Administrator, UserRole.Manager);

        sessionService.EndSession();

        Assert.Multiple(() =>
        {
            Assert.That(hasManagerRole, Is.True);
            Assert.That(hasAnyManagementRole, Is.True);
            Assert.That(sessionService.IsAuthenticated, Is.False);
            Assert.That(sessionService.CurrentUser, Is.Null);
            Assert.That(sessionService.HasRole(UserRole.Manager), Is.False);
            Assert.That(sessionService.HasAnyRole(UserRole.Manager), Is.False);
        });
    }

    [Test]
    public async Task AuditLogService_LogAsync_UsesSessionExplicitUserOrLoginAndSupportsQueries()
    {
        var services = TestServiceFactory.Create();
        var admin = await GetSeedUserAsync(services, "admin");
        var employee = await GetSeedUserAsync(services, "employee");
        services.SessionService.StartSession(admin);

        await services.AuditLogService.LogAsync(
            AuditActionType.DataViewed,
            true,
            "Просмотр",
            "Из сессии");
        await services.AuditLogService.LogAsync(
            AuditActionType.UserUpdated,
            true,
            "Изменение",
            "Явный пользователь",
            user: employee);
        services.SessionService.EndSession();
        await services.AuditLogService.LogAsync(
            AuditActionType.UserLoginFailed,
            false,
            "Ошибка входа",
            "Логин передан явно",
            login: "missing.user");

        var allEntries = await services.AuditLogService.GetAllAsync();
        var employeeEntries = await services.AuditLogService.GetByUserLoginAsync("EMPLOYEE");
        var failedLoginEntries = await services.AuditLogService.GetByActionTypeAsync(
            AuditActionType.UserLoginFailed);

        Assert.Multiple(() =>
        {
            Assert.That(allEntries, Has.Count.EqualTo(3));
            Assert.That(allEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.UserLogin == "admin" && entry.Details == "Из сессии"));
            Assert.That(employeeEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.UserLogin == "employee" && entry.Details == "Явный пользователь"));
            Assert.That(failedLoginEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "missing.user"
                && entry.Details == "Логин передан явно"));
        });
    }

    private static async Task<AppUser> GetSeedUserAsync(TestAppServices services, string login)
    {
        var user = await services.UserRepository.GetByLoginAsync(login);

        Assert.That(user, Is.Not.Null, $"Seed user '{login}' must exist.");

        return user!;
    }
}
