using MBappe.Common;
using MBappe.Models;
using MBappe.Tests.TestInfrastructure;
using MBappe.ViewModels.Analytics;

namespace MBappe.Tests.ViewModels;

[TestFixture]
public class AnalyticsViewModelTests
{
    [Test]
    public async Task GenerateReportCommand_WithIsoDateFormat_LoadsReportAndFormattedSummaryTexts()
    {
        var fixture = await CreateAnalyticsFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);
        var viewModel = new AnalyticsViewModel(fixture.Services.AnalyticsService);
        await WaitForInitialReportAsync(viewModel);

        viewModel.PeriodStartText = "2026-01-01";
        viewModel.PeriodEndText = "2026-01-31";

        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.StatusMessage, Is.EqualTo("Аналитический отчет сформирован"));
            Assert.That(viewModel.HasSummary, Is.True);
            Assert.That(viewModel.HasNoReport, Is.False);
            Assert.That(viewModel.Summary?.PeriodStart, Is.EqualTo(new DateTime(2026, 1, 1)));
            Assert.That(viewModel.Summary?.PeriodEnd, Is.EqualTo(new DateTime(2026, 1, 31)));
            Assert.That(viewModel.EmployeeRows, Has.Count.EqualTo(1));
            Assert.That(viewModel.Insights, Has.Count.EqualTo(6));
            Assert.That(viewModel.ActiveEmployeesText, Is.EqualTo("1/1"));
            Assert.That(viewModel.AverageKpiText, Is.EqualTo("75%"));
            Assert.That(viewModel.LearningProgressText, Is.EqualTo("50%"));
            Assert.That(viewModel.PayableBonusText, Is.EqualTo("1500"));
            Assert.That(viewModel.KpiPrimaryText, Is.EqualTo("0/1 выполнено"));
            Assert.That(viewModel.LearningPrimaryText, Is.EqualTo("0/1 завершено"));
            Assert.That(viewModel.MotivationSecondaryText, Is.EqualTo("На утверждении: 1, утверждено: 0, отклонено: 0, выплачено: 0"));
        });
    }

    [Test]
    public async Task GenerateReportCommand_WithInvalidDatesShowsValidationMessageAndKeepsPreviousReport()
    {
        var fixture = await CreateAnalyticsFixtureAsync();
        fixture.Services.SessionService.StartSession(fixture.AdminUser);
        var viewModel = new AnalyticsViewModel(fixture.Services.AnalyticsService);
        await WaitForInitialReportAsync(viewModel);

        var previousSummary = viewModel.Summary;
        viewModel.PeriodStartText = "31.01.2026";
        viewModel.PeriodEndText = "01.01.2026";

        await viewModel.GenerateReportCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.StatusMessage, Is.EqualTo("Дата окончания периода не может быть раньше даты начала"));
            Assert.That(viewModel.Summary, Is.SameAs(previousSummary));
            Assert.That(viewModel.HasStatusMessage, Is.True);
        });
    }

    [Test]
    public void EmployeeAnalyticsRowViewModel_FormatsDomainRowForUi()
    {
        var row = new EmployeeAnalyticsRow(
            Guid.NewGuid(),
            "Иван Петров",
            "E-001",
            "Engineering",
            "Developer",
            EmployeeStatus.SickLeave,
            69.678,
            3,
            1,
            45.555,
            2,
            1,
            1234.5m,
            500m,
            ["Низкий KPI", "Есть просроченные KPI"]);

        var viewModel = new EmployeeAnalyticsRowViewModel(row);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.FullName, Is.EqualTo("Иван Петров"));
            Assert.That(viewModel.PersonnelNumber, Is.EqualTo("E-001"));
            Assert.That(viewModel.StatusTitle, Is.EqualTo("На больничном"));
            Assert.That(viewModel.AverageKpiText, Is.EqualTo("69,68%"));
            Assert.That(viewModel.KpiDetailsText, Is.EqualTo("Всего: 3, просрочено: 1"));
            Assert.That(viewModel.LearningProgressText, Is.EqualTo("45,56%"));
            Assert.That(viewModel.LearningDetailsText, Is.EqualTo("1/2 завершено"));
            Assert.That(viewModel.PayableBonusText, Is.EqualTo("1234,5"));
            Assert.That(viewModel.PaidBonusText, Is.EqualTo("Выплачено: 500"));
            Assert.That(viewModel.ProblemFlagsText, Is.EqualTo("Низкий KPI, Есть просроченные KPI"));
        });
    }

    private static async Task<AnalyticsViewModelFixture> CreateAnalyticsFixtureAsync()
    {
        var services = TestServiceFactory.Create();
        var admin = await GetSeedUserAsync(services, "admin");
        var employee = new EmployeeProfile
        {
            UserId = admin.Id,
            PersonnelNumber = "A-001",
            FullName = "Администратор",
            Department = "IT",
            Position = "Administrator",
            Status = EmployeeStatus.Active
        };
        var course = new LearningCourse
        {
            Title = "Analytics course",
            Status = LearningCourseStatus.Active
        };

        await services.EmployeeRepository.AddAsync(employee);
        await services.KpiRepository.AddAsync(new KpiItem
        {
            EmployeeId = employee.Id,
            Title = "Analytics KPI",
            TargetValue = 100,
            ActualValue = 75,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            Status = KpiStatus.InProgress
        });
        await services.LearningRepository.AddCourseAsync(course);
        await services.LearningRepository.AddAssignmentAsync(new LearningAssignment
        {
            CourseId = course.Id,
            EmployeeId = employee.Id,
            AssignedAt = new DateTime(2026, 1, 5),
            DueDate = new DateTime(2026, 1, 31),
            ProgressPercent = 50,
            Status = LearningAssignmentStatus.InProgress
        });
        await services.MotivationBonusRepository.AddAsync(new MotivationBonus
        {
            EmployeeId = employee.Id,
            ProgramId = Guid.NewGuid(),
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            FinalAmount = 1500m,
            Status = MotivationBonusStatus.PendingApproval
        });

        return new AnalyticsViewModelFixture(services, admin);
    }

    private static async Task WaitForInitialReportAsync(AnalyticsViewModel viewModel)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (!viewModel.IsBusy && viewModel.HasStatusMessage)
                return;

            await Task.Delay(20);
        }

        Assert.Fail("Initial analytics report did not finish in time.");
    }

    private static async Task<AppUser> GetSeedUserAsync(TestAppServices services, string login)
    {
        var user = await services.UserRepository.GetByLoginAsync(login);

        Assert.That(user, Is.Not.Null, $"Seed user '{login}' must exist.");

        return user!;
    }

    private sealed record AnalyticsViewModelFixture(
        TestAppServices Services,
        AppUser AdminUser);
}
