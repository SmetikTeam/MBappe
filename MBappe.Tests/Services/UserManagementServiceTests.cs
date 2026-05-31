using MBappe.Common;
using MBappe.Models;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests.Services;

[TestFixture]
public class UserManagementServiceTests
{
    [Test]
    public async Task GetAllUsersAsync_AsHr_ReturnsUsersAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.UserManagementService.GetAllUsersAsync();

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.DataViewed);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Users, Is.Not.Null);
            Assert.That(result.Users, Has.Count.EqualTo(4));
            Assert.That(result.Users!, Has.One.Matches<AppUser>(user => user.Login == "admin"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "hr"
                && entry.Message == "Получен список пользователей"));
        });
    }

    [Test]
    public async Task GetAllUsersAsync_AsEmployee_ReturnsAccessDeniedAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.UserManagementService.GetAllUsersAsync();

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Users, Is.Null);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для просмотра списка пользователей"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "employee"
                && entry.Details == "Попытка получить список пользователей"));
        });
    }

    [Test]
    public async Task CreateUserAsync_AsAdministrator_CreatesActiveUserWithHashedPasswordAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.UserManagementService.CreateUserAsync(new CreateUserRequest
        {
            Login = " new.hr ",
            Email = " new.hr@mbappe.local ",
            FullName = " New HR ",
            Password = "strong-password",
            ConfirmPassword = "strong-password",
            Role = UserRole.HrSpecialist
        });

        var storedUser = await fixture.Services.UserRepository.GetByLoginAsync("new.hr");
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.UserCreated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.User, Is.SameAs(storedUser));
            Assert.That(storedUser, Is.Not.Null);
            Assert.That(storedUser!.Email, Is.EqualTo("new.hr@mbappe.local"));
            Assert.That(storedUser.FullName, Is.EqualTo("New HR"));
            Assert.That(storedUser.Role, Is.EqualTo(UserRole.HrSpecialist));
            Assert.That(storedUser.IsActive, Is.True);
            Assert.That(storedUser.PasswordHash, Is.Not.Empty);
            Assert.That(storedUser.PasswordSalt, Is.Not.Empty);
            Assert.That(fixture.Services.PasswordHasher.VerifyPassword(
                "strong-password",
                storedUser.PasswordSalt,
                storedUser.PasswordHash), Is.True);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Details != null
                && entry.Details.Contains("new.hr")));
        });
    }

    [Test]
    public async Task CreateUserAsync_AsManager_ReturnsAccessDeniedAndDoesNotCreateUser()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.ManagerUser);

        var result = await fixture.Services.UserManagementService.CreateUserAsync(new CreateUserRequest
        {
            Login = "denied.user",
            Email = "denied.user@mbappe.local",
            FullName = "Denied User",
            Password = "12345",
            ConfirmPassword = "12345",
            Role = UserRole.Employee
        });

        var storedUser = await fixture.Services.UserRepository.GetByLoginAsync("denied.user");
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для создания пользователя"));
            Assert.That(storedUser, Is.Null);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "manager"
                && entry.Details == "Попытка создать пользователя"));
        });
    }

    [Test]
    public async Task CreateUserAsync_WithDuplicateLoginOrEmail_ReturnsValidationFailure()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var duplicateLoginResult = await fixture.Services.UserManagementService.CreateUserAsync(new CreateUserRequest
        {
            Login = "admin",
            Email = "unique.admin.copy@mbappe.local",
            FullName = "Duplicate Login",
            Password = "12345",
            ConfirmPassword = "12345",
            Role = UserRole.Employee
        });
        var duplicateEmailResult = await fixture.Services.UserManagementService.CreateUserAsync(new CreateUserRequest
        {
            Login = "unique.admin.copy",
            Email = "admin@mbappe.local",
            FullName = "Duplicate Email",
            Password = "12345",
            ConfirmPassword = "12345",
            Role = UserRole.Employee
        });

        var createdUser = await fixture.Services.UserRepository.GetByLoginAsync("unique.admin.copy");

        Assert.Multiple(() =>
        {
            Assert.That(duplicateLoginResult.Success, Is.False);
            Assert.That(duplicateLoginResult.Message, Is.EqualTo("Пользователь с таким логином уже существует"));
            Assert.That(duplicateEmailResult.Success, Is.False);
            Assert.That(duplicateEmailResult.Message, Is.EqualTo("Пользователь с такой почтой уже существует"));
            Assert.That(createdUser, Is.Null);
        });
    }

    [Test]
    public async Task CreateUserAsync_WithInvalidPasswordConfirmation_ReturnsValidationFailure()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.UserManagementService.CreateUserAsync(new CreateUserRequest
        {
            Login = "invalid.password",
            Email = "invalid.password@mbappe.local",
            FullName = "Invalid Password",
            Password = "12345",
            ConfirmPassword = "54321",
            Role = UserRole.Employee
        });

        var storedUser = await fixture.Services.UserRepository.GetByLoginAsync("invalid.password");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Пароли не совпадают"));
            Assert.That(storedUser, Is.Null);
        });
    }

    [Test]
    public async Task UpdateUserAsync_AsHr_UpdatesUserAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.UserManagementService.UpdateUserAsync(new UpdateUserRequest
        {
            UserId = fixture.EmployeeUser.Id,
            FullName = " Updated Employee ",
            Email = " updated.employee@mbappe.local "
        });

        var storedUser = await fixture.Services.UserRepository.GetByIdAsync(fixture.EmployeeUser.Id);
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.UserUpdated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.User, Is.SameAs(storedUser));
            Assert.That(storedUser!.FullName, Is.EqualTo("Updated Employee"));
            Assert.That(storedUser.Email, Is.EqualTo("updated.employee@mbappe.local"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "hr"
                && entry.Details == "Изменен пользователь: employee"));
        });
    }

    [Test]
    public async Task UpdateUserAsync_WithDuplicateEmail_ReturnsValidationFailure()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.UserManagementService.UpdateUserAsync(new UpdateUserRequest
        {
            UserId = fixture.EmployeeUser.Id,
            FullName = "Employee Duplicate Email",
            Email = "manager@mbappe.local"
        });

        var storedUser = await fixture.Services.UserRepository.GetByIdAsync(fixture.EmployeeUser.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Почта уже используется другим пользователем"));
            Assert.That(storedUser!.Email, Is.EqualTo("employee@mbappe.local"));
        });
    }

    [Test]
    public async Task ChangeUserRoleAsync_AsAdministrator_ChangesRoleAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.UserManagementService.ChangeUserRoleAsync(
            fixture.EmployeeUser.Id,
            UserRole.Manager);

        var storedUser = await fixture.Services.UserRepository.GetByIdAsync(fixture.EmployeeUser.Id);
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.UserRoleChanged);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.User, Is.SameAs(storedUser));
            Assert.That(storedUser!.Role, Is.EqualTo(UserRole.Manager));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Details != null
                && entry.Details.Contains("старая роль: Employee")
                && entry.Details.Contains("новая роль: Manager")));
        });
    }

    [Test]
    public async Task ChangeUserRoleAsync_AsHr_ReturnsAccessDeniedAndKeepsRole()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var result = await fixture.Services.UserManagementService.ChangeUserRoleAsync(
            fixture.EmployeeUser.Id,
            UserRole.Manager);

        var storedUser = await fixture.Services.UserRepository.GetByIdAsync(fixture.EmployeeUser.Id);
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Только администратор может изменять роли пользователей"));
            Assert.That(storedUser!.Role, Is.EqualTo(UserRole.Employee));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "hr"
                && entry.Details == "Попытка изменить роль пользователя"));
        });
    }

    [Test]
    public async Task BlockAndUnblockUserAsync_AsHr_TogglesUserStateAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.HrUser);

        var blockResult = await fixture.Services.UserManagementService.BlockUserAsync(fixture.EmployeeUser.Id);
        var isActiveAfterBlock = fixture.EmployeeUser.IsActive;
        var unblockResult = await fixture.Services.UserManagementService.UnblockUserAsync(fixture.EmployeeUser.Id);

        var blockAuditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.UserBlocked);
        var unblockAuditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.UserUnblocked);

        Assert.Multiple(() =>
        {
            Assert.That(blockResult.Success, Is.True);
            Assert.That(isActiveAfterBlock, Is.False);
            Assert.That(unblockResult.Success, Is.True);
            Assert.That(fixture.EmployeeUser.IsActive, Is.True);
            Assert.That(blockAuditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "hr"
                && entry.Details == "Заблокирован пользователь: employee"));
            Assert.That(unblockAuditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "hr"
                && entry.Details == "Разблокирован пользователь: employee"));
        });
    }

    [Test]
    public async Task BlockUserAsync_WhenBlockingCurrentUser_ReturnsValidationFailure()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.UserManagementService.BlockUserAsync(fixture.AdminUser.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Нельзя заблокировать собственный аккаунт"));
            Assert.That(fixture.AdminUser.IsActive, Is.True);
        });
    }

    [Test]
    public async Task UnblockUserAsync_WhenUserAlreadyActive_ReturnsValidationFailure()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.UserManagementService.UnblockUserAsync(fixture.EmployeeUser.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Пользователь уже активен"));
            Assert.That(fixture.EmployeeUser.IsActive, Is.True);
        });
    }

    private static async Task<UserManagementFixture> CreateFixtureAsync()
    {
        var services = TestServiceFactory.Create();
        var admin = await GetSeedUserAsync(services, "admin");
        var hr = await GetSeedUserAsync(services, "hr");
        var manager = await GetSeedUserAsync(services, "manager");
        var employee = await GetSeedUserAsync(services, "employee");

        return new UserManagementFixture(
            services,
            admin,
            hr,
            manager,
            employee);
    }

    private static async Task<AppUser> GetSeedUserAsync(TestAppServices services, string login)
    {
        var user = await services.UserRepository.GetByLoginAsync(login);

        Assert.That(user, Is.Not.Null, $"Seed user '{login}' must exist.");

        return user!;
    }

    private sealed record UserManagementFixture(
        TestAppServices Services,
        AppUser AdminUser,
        AppUser HrUser,
        AppUser ManagerUser,
        AppUser EmployeeUser);
}
