using MBappe.Models;
using MBappe.Tests.TestInfrastructure;
using MBappe.ViewModels;
using MBappe.ViewModels.Analytics;
using MBappe.ViewModels.Shell;

namespace MBappe.Tests.ViewModels;

[TestFixture]
public class MainShellViewModelTests
{
    [TestCase("admin", UserRole.Administrator)]
    [TestCase("hr", UserRole.HrSpecialist)]
    [TestCase("manager", UserRole.Manager)]
    [TestCase("employee", UserRole.Employee)]
    public async Task Constructor_ForEveryRole_AddsAnalyticsNavigationItem(string login, UserRole expectedRole)
    {
        var services = TestServiceFactory.Create();
        var user = await GetSeedUserAsync(services, login);
        services.SessionService.StartSession(user);

        var shell = CreateShell(services);

        var analyticsItem = shell.NavigationItems.Single(item => item.Title == "Аналитика");
        var analyticsPage = analyticsItem.CreateViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(shell.CurrentUser, Is.SameAs(user));
            Assert.That(shell.CurrentUserRole, Is.EqualTo(DisplayNames.ForRole(expectedRole)));
            Assert.That(analyticsItem.IconKey, Is.EqualTo(NavigationIconPack.Analytics));
            Assert.That(analyticsItem.Description, Is.EqualTo("Отчеты по персоналу"));
            Assert.That(analyticsPage, Is.TypeOf<AnalyticsViewModel>());
        });
    }

    [Test]
    public async Task Constructor_ForAdministrator_AddsUsersEmployeesAuditAndAnalyticsNavigation()
    {
        var services = TestServiceFactory.Create();
        var user = await GetSeedUserAsync(services, "admin");
        services.SessionService.StartSession(user);

        var shell = CreateShell(services);

        Assert.That(shell.NavigationItems.Select(item => item.Title), Is.EqualTo(new[]
        {
            "Пользователи",
            "Сотрудники",
            "KPI",
            "Обучение",
            "Мотивация",
            "Аналитика",
            "Журнал"
        }));
    }

    [Test]
    public async Task LogoutCommand_EndsSessionAndOpensLoginWithMessage()
    {
        var services = TestServiceFactory.Create();
        var user = await GetSeedUserAsync(services, "admin");
        services.SessionService.StartSession(user);
        string? openedMessage = null;
        var shell = CreateShell(services, message => openedMessage = message);

        await shell.LogoutCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(services.SessionService.IsAuthenticated, Is.False);
            Assert.That(openedMessage, Is.EqualTo("Вы вышли из системы."));
        });
    }

    private static MainShellViewModel CreateShell(
        TestAppServices services,
        Action<string?>? openLogin = null)
    {
        return new MainShellViewModel(
            services.AuthService,
            services.SessionService,
            services.UserManagementService,
            services.EmployeeService,
            services.KpiService,
            services.LearningService,
            services.MotivationService,
            services.AnalyticsService,
            services.AuditLogService,
            openLogin ?? (_ => { }));
    }

    private static async Task<AppUser> GetSeedUserAsync(TestAppServices services, string login)
    {
        var user = await services.UserRepository.GetByLoginAsync(login);

        Assert.That(user, Is.Not.Null, $"Seed user '{login}' must exist.");

        return user!;
    }
}
