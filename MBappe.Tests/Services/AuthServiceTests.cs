using MBappe.Common;
using MBappe.Models;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    [Test]
    public async Task RegisterAsync_WithValidRequest_CreatesActiveUserWithHashedPasswordAndWritesAudit()
    {
        var services = TestServiceFactory.Create();

        var result = await services.AuthService.RegisterAsync(new RegisterRequest
        {
            Login = " new.manager ",
            Email = " new.manager@mbappe.local ",
            FullName = " New Manager ",
            Password = "strong-password",
            ConfirmPassword = "strong-password",
            Role = UserRole.Manager
        });

        var user = await services.UserRepository.GetByLoginAsync("new.manager");
        var auditEntries = await services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.UserRegistrationSuccess);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.User, Is.SameAs(user));
            Assert.That(user, Is.Not.Null);
            Assert.That(user!.Email, Is.EqualTo("new.manager@mbappe.local"));
            Assert.That(user.FullName, Is.EqualTo("New Manager"));
            Assert.That(user.Role, Is.EqualTo(UserRole.Manager));
            Assert.That(user.IsActive, Is.True);
            Assert.That(user.PasswordHash, Is.Not.Empty);
            Assert.That(user.PasswordSalt, Is.Not.Empty);
            Assert.That(services.PasswordHasher.VerifyPassword(
                "strong-password",
                user.PasswordSalt,
                user.PasswordHash), Is.True);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess && entry.UserLogin == "new.manager"));
        });
    }

    [Test]
    public async Task RegisterAsync_WithDuplicateLogin_ReturnsFailureAndWritesAudit()
    {
        var services = TestServiceFactory.Create();

        var result = await services.AuthService.RegisterAsync(new RegisterRequest
        {
            Login = "admin",
            Email = "duplicate@mbappe.local",
            FullName = "Duplicate User",
            Password = "12345",
            ConfirmPassword = "12345",
            Role = UserRole.Employee
        });

        var auditEntries = await services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.UserRegistrationFailed);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.User, Is.Null);
            Assert.That(result.Message, Is.EqualTo("Пользователь с таким логином уже существует"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Details == "Пользователь с таким логином уже существует"));
        });
    }

    [Test]
    public async Task LoginAsync_WithWrongPassword_DoesNotStartSessionAndWritesFailedAudit()
    {
        var services = TestServiceFactory.Create();

        var result = await services.AuthService.LoginAsync("admin", "wrong-password");
        var auditEntries = await services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.UserLoginFailed);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Неверный пароль"));
            Assert.That(services.SessionService.IsAuthenticated, Is.False);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Details == "Введен неверный пароль"));
        });
    }

    [Test]
    public async Task LogoutAsync_WhenAuthenticated_EndsSessionAndWritesAudit()
    {
        var services = TestServiceFactory.Create();
        var loginResult = await services.AuthService.LoginAsync("admin", "12345");

        await services.AuthService.LogoutAsync();

        var auditEntries = await services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.UserLogout);

        Assert.Multiple(() =>
        {
            Assert.That(loginResult.Success, Is.True);
            Assert.That(services.SessionService.IsAuthenticated, Is.False);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Message == "Пользователь вышел из системы"));
        });
    }
}
