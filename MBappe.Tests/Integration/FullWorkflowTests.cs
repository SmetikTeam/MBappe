using MBappe.Common;
using MBappe.Models;
using MBappe.Tests.TestInfrastructure;

namespace MBappe.Tests.Integration;

[TestFixture]
public class FullWorkflowTests
{
    [Test]
    public async Task AdminWorkflow_CreatesEmployeeKpiLearningBonusAndAnalyticsReport()
    {
        var services = TestServiceFactory.Create();

        var login = await services.AuthService.LoginAsync("admin", "12345");

        Assert.That(login.Success, Is.True);

        var createUser = await services.UserManagementService.CreateUserAsync(new CreateUserRequest
        {
            Login = "workflow-user",
            Email = "workflow-user@mbappe.local",
            FullName = "Workflow User",
            Password = "12345",
            ConfirmPassword = "12345",
            Role = UserRole.Employee
        });

        Assert.That(createUser.Success, Is.True);

        var users = await services.UserManagementService.GetAllUsersAsync();

        Assert.That(users.Users, Is.Not.Null);

        var user = users.Users!
            .First(user => user.Login == "workflow-user");

        var createEmployee = await services.EmployeeService.CreateEmployeeAsync(new CreateEmployeeRequest
        {
            UserId = user.Id,
            PersonnelNumber = "WF-001",
            FullName = "Workflow User",
            Position = "Developer",
            Department = "IT",
            Email = "workflow-user@mbappe.local",
            Phone = "+7 000 000-00-01",
            HireDate = DateTime.Today
        });

        Assert.That(createEmployee.Success, Is.True);
        Assert.That(createEmployee.Employee, Is.Not.Null);

        var employee = createEmployee.Employee!;

        var periodStart = new DateTime(2026, 6, 1);
        var periodEnd = new DateTime(2026, 6, 30);

        var createKpi = await services.KpiService.CreateKpiAsync(new CreateKpiRequest
        {
            EmployeeId = employee.Id,
            Title = "Закрыть задачи",
            Description = "KPI для полного бизнес-сценария",
            TargetValue = 20,
            ActualValue = 18,
            Unit = "задач",
            WeightPercent = 100,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });

        Assert.That(createKpi.Success, Is.True);
        Assert.That(createKpi.Kpi, Is.Not.Null);
        Assert.That(createKpi.Kpi!.CompletionPercent, Is.EqualTo(90));

        var updateKpiProgress = await services.KpiService.UpdateKpiProgressAsync(new UpdateKpiProgressRequest
        {
            KpiId = createKpi.Kpi.Id,
            ActualValue = 20
        });

        Assert.That(updateKpiProgress.Success, Is.True);
        Assert.That(updateKpiProgress.Kpi, Is.Not.Null);
        Assert.That(updateKpiProgress.Kpi!.CompletionPercent, Is.EqualTo(100));
        Assert.That(updateKpiProgress.Kpi.Status, Is.EqualTo(KpiStatus.Completed));

        var createCourse = await services.LearningService.CreateCourseAsync(new CreateLearningCourseRequest
        {
            Title = "Workflow Course",
            Description = "Курс для полного бизнес-сценария",
            Format = LearningFormat.Online,
            Provider = "MBappe Academy",
            DurationHours = 2
        });

        Assert.That(createCourse.Success, Is.True);
        Assert.That(createCourse.Course, Is.Not.Null);

        var activateCourse = await services.LearningService.UpdateCourseAsync(new UpdateLearningCourseRequest
        {
            CourseId = createCourse.Course!.Id,
            Title = createCourse.Course.Title,
            Description = createCourse.Course.Description,
            Format = createCourse.Course.Format,
            Provider = createCourse.Course.Provider,
            DurationHours = createCourse.Course.DurationHours,
            Status = LearningCourseStatus.Active
        });

        Assert.That(activateCourse.Success, Is.True);

        var assignCourse = await services.LearningService.AssignCourseAsync(new AssignLearningCourseRequest
        {
            CourseId = createCourse.Course.Id,
            EmployeeId = employee.Id,
            DueDate = DateTime.Today.AddDays(7)
        });

        Assert.That(assignCourse.Success, Is.True);
        Assert.That(assignCourse.Assignment, Is.Not.Null);

        var updateLearningProgress = await services.LearningService.UpdateAssignmentProgressAsync(
            new UpdateLearningAssignmentProgressRequest
            {
                AssignmentId = assignCourse.Assignment!.Id,
                ProgressPercent = 100,
                Score = 95
            });

        Assert.That(updateLearningProgress.Success, Is.True);
        Assert.That(updateLearningProgress.Assignment, Is.Not.Null);
        Assert.That(updateLearningProgress.Assignment!.ProgressPercent, Is.EqualTo(100));
        Assert.That(updateLearningProgress.Assignment.Status, Is.EqualTo(LearningAssignmentStatus.Completed));

        var programs = await services.MotivationService.GetProgramsAsync();

        Assert.That(programs.Success, Is.True);
        Assert.That(programs.Programs, Is.Not.Null);
        Assert.That(programs.Programs, Is.Not.Empty);

        var program = programs.Programs!.First();

        var calculateBonus = await services.MotivationService.CalculateBonusAsync(new CalculateBonusRequest
        {
            EmployeeId = employee.Id,
            ProgramId = program.Id,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });

        Assert.That(calculateBonus.Success, Is.True);
        Assert.That(calculateBonus.Bonus, Is.Not.Null);
        Assert.That(calculateBonus.Bonus!.EfficiencyPercent, Is.EqualTo(100));
        Assert.That(calculateBonus.Bonus.FinalAmount, Is.GreaterThan(0));
        Assert.That(calculateBonus.Bonus.Status, Is.EqualTo(MotivationBonusStatus.PendingApproval));

        var approveBonus = await services.MotivationService.ApproveBonusAsync(calculateBonus.Bonus.Id);

        Assert.That(approveBonus.Success, Is.True);
        Assert.That(approveBonus.Bonus, Is.Not.Null);
        Assert.That(approveBonus.Bonus!.Status, Is.EqualTo(MotivationBonusStatus.Approved));

        var report = await services.AnalyticsService.GetDashboardReportAsync(periodStart, periodEnd);

        Assert.That(report.Success, Is.True);
        Assert.That(report.Report, Is.Not.Null);

        var analyticsReport = report.Report!;

        Assert.That(analyticsReport.Summary.TotalEmployees, Is.GreaterThanOrEqualTo(1));
        Assert.That(analyticsReport.Summary.TotalKpis, Is.GreaterThanOrEqualTo(1));
        Assert.That(analyticsReport.Summary.TotalLearningAssignments, Is.GreaterThanOrEqualTo(1));
        Assert.That(analyticsReport.Summary.TotalBonuses, Is.GreaterThanOrEqualTo(1));

        Assert.That(
            analyticsReport.EmployeeRows.Any(row => row.EmployeeId == employee.Id),
            Is.True);

        var employeeAnalyticsRow = analyticsReport.EmployeeRows
            .First(row => row.EmployeeId == employee.Id);

        Assert.That(employeeAnalyticsRow.AverageKpiPercent, Is.EqualTo(100));
        Assert.That(employeeAnalyticsRow.LearningProgressPercent, Is.EqualTo(100));
        Assert.That(employeeAnalyticsRow.PayableBonusAmount, Is.GreaterThan(0));

        var auditEntries = await services.AuditLogService.GetAllAsync();

        Assert.That(auditEntries, Is.Not.Empty);
        Assert.That(auditEntries.Any(entry => entry.ActionType == AuditActionType.UserLoginSuccess), Is.True);
        Assert.That(auditEntries.Any(entry => entry.ActionType == AuditActionType.EmployeeCreated), Is.True);
        Assert.That(auditEntries.Any(entry => entry.ActionType == AuditActionType.KpiCreated), Is.True);
        Assert.That(auditEntries.Any(entry => entry.ActionType == AuditActionType.LearningAssigned), Is.True);
        Assert.That(auditEntries.Any(entry => entry.ActionType == AuditActionType.BonusCalculated), Is.True);
        Assert.That(auditEntries.Any(entry => entry.ActionType == AuditActionType.AnalyticsReportGenerated), Is.True);
    }
}