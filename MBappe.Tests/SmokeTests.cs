using MBappe.Models;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests;

[TestFixture]
public class SmokeTests
{
    [Test]
    public async Task TestFactory_CreatesIsolatedServiceGraph()
    {
        var first = TestServiceFactory.Create();
        var second = TestServiceFactory.Create();

        var admin = await first.UserRepository.GetByLoginAsync("admin");
        Assert.That(admin, Is.Not.Null);

        first.SessionService.StartSession(admin!);

        await first.EmployeeRepository.AddAsync(new EmployeeProfile
        {
            UserId = admin!.Id,
            PersonnelNumber = "T-001",
            FullName = "Test Admin",
            Department = "Testing",
            Position = "Administrator"
        });

        var firstEmployees = await first.EmployeeRepository.GetAllAsync();
        var secondEmployees = await second.EmployeeRepository.GetAllAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first.SessionService.CurrentUser, Is.SameAs(admin));
            Assert.That(firstEmployees, Has.Count.EqualTo(1));
            Assert.That(secondEmployees, Is.Empty);
        });
    }

    [Test]
    public async Task AuthService_Login_WithSeedAdminStartsSessionAndWritesAudit()
    {
        var services = TestServiceFactory.Create();

        var result = await services.AuthService.LoginAsync("admin", "12345");
        var auditEntries = await services.AuditLogRepository.GetAllAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(services.SessionService.IsAuthenticated, Is.True);
            Assert.That(services.SessionService.CurrentUser?.Login, Is.EqualTo("admin"));
            Assert.That(auditEntries, Has.One.Matches<AuditLogEntry>(entry =>
                entry.ActionType == AuditActionType.UserLoginSuccess && entry.IsSuccess));
        });
    }
}
