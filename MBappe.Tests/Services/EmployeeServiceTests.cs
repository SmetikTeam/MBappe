using MBappe.Common;
using MBappe.Models;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests.Services;

[TestFixture]
public class EmployeeServiceTests
{
    [Test]
    public async Task CreateEmployeeAsync_AsAdministrator_CreatesProfileAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        var newUser = await RegisterUserAsync(fixture.Services, "new.employee", UserRole.Employee);
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.EmployeeService.CreateEmployeeAsync(new CreateEmployeeRequest
        {
            UserId = newUser.Id,
            PersonnelNumber = " E-100 ",
            FullName = " New Employee ",
            Position = " Developer ",
            Department = " Engineering ",
            ManagerEmployeeId = fixture.ManagerProfile.Id,
            Email = " new.employee@mbappe.local ",
            Phone = "+7 900 000-00-00",
            HireDate = new DateTime(2026, 2, 10)
        });

        var storedEmployee = await fixture.Services.EmployeeRepository.GetByUserIdAsync(newUser.Id);
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.EmployeeCreated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Employee, Is.SameAs(storedEmployee));
            Assert.That(storedEmployee, Is.Not.Null);
            Assert.That(storedEmployee!.PersonnelNumber, Is.EqualTo("E-100"));
            Assert.That(storedEmployee.FullName, Is.EqualTo("New Employee"));
            Assert.That(storedEmployee.Position, Is.EqualTo("Developer"));
            Assert.That(storedEmployee.Department, Is.EqualTo("Engineering"));
            Assert.That(storedEmployee.ManagerEmployeeId, Is.EqualTo(fixture.ManagerProfile.Id));
            Assert.That(storedEmployee.Status, Is.EqualTo(EmployeeStatus.Active));
            Assert.That(storedEmployee.HireDate, Is.EqualTo(new DateTime(2026, 2, 10)));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess
                && entry.UserLogin == "admin"
                && entry.Details != null
                && entry.Details.Contains("E-100")));
        });
    }

    [Test]
    public async Task CreateEmployeeAsync_AsEmployee_ReturnsAccessDeniedAndWritesAudit()
    {
        var fixture = await CreateFixtureAsync();
        var newUser = await RegisterUserAsync(fixture.Services, "denied.employee", UserRole.Employee);
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.EmployeeService.CreateEmployeeAsync(new CreateEmployeeRequest
        {
            UserId = newUser.Id,
            PersonnelNumber = "E-101",
            FullName = "Denied Employee",
            Position = "Developer",
            Department = "Engineering",
            Email = "denied.employee@mbappe.local"
        });

        var storedEmployee = await fixture.Services.EmployeeRepository.GetByUserIdAsync(newUser.Id);
        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для создания сотрудника"));
            Assert.That(storedEmployee, Is.Null);
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess
                && entry.UserLogin == "employee"
                && entry.Details == "Попытка создать профиль сотрудника"));
        });
    }

    [Test]
    public async Task GetAllEmployeesAsync_AsEmployee_ReturnsAccessDenied()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.EmployeeService.GetAllEmployeesAsync();

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(AuditActionType.AccessDenied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Employees, Is.Null);
            Assert.That(result.Message, Is.EqualTo("Недостаточно прав для просмотра списка сотрудников"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                !entry.IsSuccess && entry.UserLogin == "employee"));
        });
    }

    [Test]
    public async Task GetEmployeeByIdAsync_AllowsEmployeeToViewOwnProfileOnly()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var ownResult = await fixture.Services.EmployeeService.GetEmployeeByIdAsync(fixture.EmployeeProfile.Id);
        var managerResult = await fixture.Services.EmployeeService.GetEmployeeByIdAsync(fixture.ManagerProfile.Id);

        Assert.Multiple(() =>
        {
            Assert.That(ownResult.Success, Is.True);
            Assert.That(ownResult.Employee, Is.SameAs(fixture.EmployeeProfile));
            Assert.That(managerResult.Success, Is.False);
            Assert.That(managerResult.Message, Is.EqualTo("Недостаточно прав для просмотра профиля сотрудника"));
        });
    }

    [Test]
    public async Task GetCurrentEmployeeProfileAsync_ReturnsProfileForCurrentUser()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.EmployeeUser);

        var result = await fixture.Services.EmployeeService.GetCurrentEmployeeProfileAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Employee, Is.SameAs(fixture.EmployeeProfile));
            Assert.That(result.Message, Is.EqualTo("Профиль сотрудника получен"));
        });
    }

    [Test]
    public async Task UpdateEmployeeAsync_WithSelfManager_ReturnsValidationFailure()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.EmployeeService.UpdateEmployeeAsync(new UpdateEmployeeRequest
        {
            EmployeeId = fixture.EmployeeProfile.Id,
            FullName = "Иван Петров",
            Position = "Developer",
            Department = "Engineering",
            ManagerEmployeeId = fixture.EmployeeProfile.Id,
            Email = "employee@mbappe.local",
            Phone = "+7 900 000-00-00"
        });

        var auditEntries = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.EmployeeUpdated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Сотрудник не может быть собственным руководителем"));
            Assert.That(auditEntries, Is.Empty);
        });
    }

    [Test]
    public async Task ChangeEmployeeStatusAsync_DismissesAndRestoresEmployeeWithAudit()
    {
        var fixture = await CreateFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var dismissResult = await fixture.Services.EmployeeService.DismissEmployeeAsync(fixture.EmployeeProfile.Id);
        var statusAfterDismiss = fixture.EmployeeProfile.Status;
        var dismissalDateAfterDismiss = fixture.EmployeeProfile.DismissalDate;
        var restoreResult = await fixture.Services.EmployeeService.RestoreEmployeeAsync(fixture.EmployeeProfile.Id);

        var dismissAudit = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.EmployeeDismissed);
        var restoreAudit = await fixture.Services.AuditLogRepository.GetByActionTypeAsync(
            AuditActionType.EmployeeRestored);

        Assert.Multiple(() =>
        {
            Assert.That(dismissResult.Success, Is.True);
            Assert.That(statusAfterDismiss, Is.EqualTo(EmployeeStatus.Dismissed));
            Assert.That(dismissalDateAfterDismiss, Is.Not.Null);
            Assert.That(restoreResult.Success, Is.True);
            Assert.That(restoreResult.Employee?.Status, Is.EqualTo(EmployeeStatus.Active));
            Assert.That(fixture.EmployeeProfile.DismissalDate, Is.Null);
            Assert.That(dismissAudit, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess && entry.UserLogin == "admin"));
            Assert.That(restoreAudit, Has.One.Matches<AuditLogEntry>(entry =>
                entry.IsSuccess && entry.UserLogin == "admin"));
        });
    }

    [Test]
    public async Task CreateEmployeeAsync_WithNonManagerAsManager_ReturnsValidationFailure()
    {
        var fixture = await CreateFixtureAsync();
        var newUser = await RegisterUserAsync(fixture.Services, "bad.manager.assignment", UserRole.Employee);
        fixture.Services.SessionService.StartSession(fixture.AdminUser);

        var result = await fixture.Services.EmployeeService.CreateEmployeeAsync(new CreateEmployeeRequest
        {
            UserId = newUser.Id,
            PersonnelNumber = "E-102",
            FullName = "Bad Manager Assignment",
            Position = "Developer",
            Department = "Engineering",
            ManagerEmployeeId = fixture.EmployeeProfile.Id,
            Email = "bad.manager.assignment@mbappe.local",
            Phone = "+7 900 000-00-00"
        });

        var storedEmployee = await fixture.Services.EmployeeRepository.GetByUserIdAsync(newUser.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Руководителем можно назначить только HR-специалиста или руководителя"));
            Assert.That(storedEmployee, Is.Null);
        });
    }

    private static async Task<EmployeeFixture> CreateFixtureAsync()
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
            Email = "manager@mbappe.local",
            Phone = "+7 900 000-00-00",
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
            Email = "employee@mbappe.local",
            Phone = "+7 900 000-00-01",
            Status = EmployeeStatus.Active
        };

        await services.EmployeeRepository.AddAsync(managerProfile);
        await services.EmployeeRepository.AddAsync(employeeProfile);

        return new EmployeeFixture(
            services,
            admin,
            manager,
            employee,
            managerProfile,
            employeeProfile);
    }

    private static async Task<AppUser> RegisterUserAsync(
        TestAppServices services,
        string login,
        UserRole role)
    {
        var result = await services.AuthService.RegisterAsync(new RegisterRequest
        {
            Login = login,
            Email = $"{login}@mbappe.local",
            FullName = login,
            Password = "12345",
            ConfirmPassword = "12345",
            Role = role
        });

        Assert.That(result.Success, Is.True, result.Message);

        return result.User!;
    }

    private static async Task<AppUser> GetSeedUserAsync(TestAppServices services, string login)
    {
        var user = await services.UserRepository.GetByLoginAsync(login);

        Assert.That(user, Is.Not.Null, $"Seed user '{login}' must exist.");

        return user!;
    }

    private sealed record EmployeeFixture(
        TestAppServices Services,
        AppUser AdminUser,
        AppUser ManagerUser,
        AppUser EmployeeUser,
        EmployeeProfile ManagerProfile,
        EmployeeProfile EmployeeProfile);
}
