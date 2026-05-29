using MBappe.Common;
using MBappe.Models;
using MBappe.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Services;

public class AnalyticsService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IKpiRepository _kpiRepository;
    private readonly ILearningRepository _learningRepository;
    private readonly IMotivationBonusRepository _bonusRepository;
    private readonly SessionService _sessionService;
    private readonly AuditLogService _auditLogService;

    public AnalyticsService(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        IKpiRepository kpiRepository,
        ILearningRepository learningRepository,
        IMotivationBonusRepository bonusRepository,
        SessionService sessionService,
        AuditLogService auditLogService)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _kpiRepository = kpiRepository;
        _learningRepository = learningRepository;
        _bonusRepository = bonusRepository;
        _sessionService = sessionService;
        _auditLogService = auditLogService;
    }

    public async Task<AnalyticsOperationResult> GetReportAsync()
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка сформировать аналитический отчет без активной сессии", "Пользователь не авторизован");

        var allEmployees = await _employeeRepository.GetAllAsync();
        var visibleEmployeesResult = await GetVisibleEmployeesAsync(currentUser, allEmployees);

        if (visibleEmployeesResult.Error is not null)
            return AnalyticsOperationResult.Fail(visibleEmployeesResult.Error);

        var visibleEmployees = visibleEmployeesResult.Employees;
        var visibleEmployeeIds = visibleEmployees
            .Select(employee => employee.Id)
            .ToHashSet();

        var allUsers = await _userRepository.GetAllAsync();
        var visibleUsers = GetVisibleUsers(currentUser, allUsers, visibleEmployees);

        var kpis = (await _kpiRepository.GetAllAsync())
            .Where(kpi => visibleEmployeeIds.Contains(kpi.EmployeeId))
            .ToList();

        var allCourses = await _learningRepository.GetAllCoursesAsync();
        var assignments = (await _learningRepository.GetAllAssignmentsAsync())
            .Where(assignment => visibleEmployeeIds.Contains(assignment.EmployeeId))
            .ToList();

        var visibleCourses = GetVisibleCourses(currentUser, allCourses, assignments);

        var bonuses = (await _bonusRepository.GetAllAsync())
            .Where(bonus => visibleEmployeeIds.Contains(bonus.EmployeeId))
            .ToList();

        var employeeSummary = BuildEmployeeSummary(visibleEmployees, visibleUsers);
        var kpiSummary = BuildKpiSummary(kpis);
        var learningSummary = BuildLearningSummary(visibleCourses, assignments);
        var motivationSummary = BuildMotivationSummary(bonuses);
        var departments = BuildDepartmentSummaries(visibleEmployees, kpis, assignments, bonuses);
        var employeeRows = BuildEmployeeRows(visibleEmployees, kpis, assignments, bonuses);
        var insights = BuildInsights(employeeSummary, kpiSummary, learningSummary, motivationSummary);

        var report = new AnalyticsReport(
            DateTime.Now,
            GetScopeTitle(currentUser),
            employeeSummary,
            kpiSummary,
            learningSummary,
            motivationSummary,
            departments,
            employeeRows,
            insights);

        await _auditLogService.LogAsync(
            AuditActionType.DataViewed,
            true,
            "Сформирован аналитический отчет",
            $"Область: {report.ScopeTitle}, сотрудников: {employeeSummary.TotalEmployees}, KPI: {kpiSummary.TotalKpis}",
            user: currentUser);

        return AnalyticsOperationResult.Ok(report, "Аналитический отчет сформирован");
    }

    private async Task<(IReadOnlyList<EmployeeProfile> Employees, string? Error)> GetVisibleEmployeesAsync(
        AppUser currentUser,
        IReadOnlyList<EmployeeProfile> allEmployees)
    {
        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            return (allEmployees, null);

        var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

        if (currentEmployee is null)
            return ([], "Для текущей учетной записи не создан профиль сотрудника");

        if (currentUser.Role == UserRole.Manager)
        {
            var employees = allEmployees
                .Where(employee => employee.Id == currentEmployee.Id || employee.ManagerEmployeeId == currentEmployee.Id)
                .ToList();

            return (employees, null);
        }

        return ([currentEmployee], null);
    }

    private static IReadOnlyList<AppUser> GetVisibleUsers(
        AppUser currentUser,
        IReadOnlyList<AppUser> allUsers,
        IReadOnlyList<EmployeeProfile> visibleEmployees)
    {
        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            return allUsers;

        var visibleUserIds = visibleEmployees
            .Select(employee => employee.UserId)
            .ToHashSet();

        return allUsers
            .Where(user => visibleUserIds.Contains(user.Id))
            .ToList();
    }

    private static IReadOnlyList<LearningCourse> GetVisibleCourses(
        AppUser currentUser,
        IReadOnlyList<LearningCourse> allCourses,
        IReadOnlyList<LearningAssignment> visibleAssignments)
    {
        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            return allCourses;

        var visibleCourseIds = visibleAssignments
            .Select(assignment => assignment.CourseId)
            .ToHashSet();

        return allCourses
            .Where(course => visibleCourseIds.Contains(course.Id))
            .ToList();
    }

    private static AnalyticsEmployeeSummary BuildEmployeeSummary(
        IReadOnlyList<EmployeeProfile> employees,
        IReadOnlyList<AppUser> users)
    {
        var departmentCount = employees
            .Select(employee => NormalizeDepartment(employee.Department))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new AnalyticsEmployeeSummary(
            employees.Count,
            employees.Count(employee => employee.Status == EmployeeStatus.Active),
            employees.Count(employee => employee.Status == EmployeeStatus.Dismissed),
            employees.Count(employee => employee.Status == EmployeeStatus.OnVacation),
            employees.Count(employee => employee.Status == EmployeeStatus.SickLeave),
            departmentCount,
            users.Count,
            users.Count(user => user.IsActive));
    }

    private static AnalyticsKpiSummary BuildKpiSummary(IReadOnlyList<KpiItem> kpis)
    {
        var activeKpis = kpis
            .Where(kpi => kpi.Status != KpiStatus.Cancelled)
            .ToList();

        var completedKpis = kpis.Count(kpi => kpi.Status == KpiStatus.Completed);

        return new AnalyticsKpiSummary(
            kpis.Count,
            kpis.Count(kpi => kpi.Status == KpiStatus.InProgress),
            completedKpis,
            kpis.Count(kpi => kpi.Status == KpiStatus.Overdue),
            kpis.Count(kpi => kpi.Status == KpiStatus.Cancelled),
            Average(activeKpis.Select(kpi => kpi.CompletionPercent)),
            Percent(completedKpis, activeKpis.Count));
    }

    private static AnalyticsLearningSummary BuildLearningSummary(
        IReadOnlyList<LearningCourse> courses,
        IReadOnlyList<LearningAssignment> assignments)
    {
        var activeAssignments = assignments.Count(assignment =>
            assignment.Status is LearningAssignmentStatus.Assigned or LearningAssignmentStatus.InProgress);
        var completedAssignments = assignments.Count(assignment =>
            assignment.Status == LearningAssignmentStatus.Completed);
        var measuredAssignments = assignments
            .Where(assignment => assignment.Status != LearningAssignmentStatus.Cancelled)
            .ToList();

        return new AnalyticsLearningSummary(
            courses.Count,
            courses.Count(course => course.Status == LearningCourseStatus.Active),
            assignments.Count,
            activeAssignments,
            completedAssignments,
            assignments.Count(assignment => assignment.Status == LearningAssignmentStatus.Cancelled),
            Average(measuredAssignments.Select(assignment => assignment.ProgressPercent)),
            Average(assignments.Where(assignment => assignment.Score is not null).Select(assignment => assignment.Score!.Value)),
            Percent(completedAssignments, measuredAssignments.Count));
    }

    private static AnalyticsMotivationSummary BuildMotivationSummary(IReadOnlyList<MotivationBonus> bonuses)
    {
        var payableStatuses = new[]
        {
            MotivationBonusStatus.PendingApproval,
            MotivationBonusStatus.Approved
        };

        var actualBonuses = bonuses
            .Where(bonus => bonus.Status is not MotivationBonusStatus.Rejected and not MotivationBonusStatus.Cancelled)
            .ToList();

        return new AnalyticsMotivationSummary(
            bonuses.Count,
            bonuses.Count(bonus => bonus.Status == MotivationBonusStatus.PendingApproval),
            bonuses.Count(bonus => bonus.Status == MotivationBonusStatus.Approved),
            bonuses.Count(bonus => bonus.Status == MotivationBonusStatus.Paid),
            bonuses.Count(bonus => bonus.Status == MotivationBonusStatus.Rejected),
            bonuses.Count(bonus => bonus.Status == MotivationBonusStatus.Cancelled),
            bonuses.Sum(bonus => bonus.CalculatedAmount),
            actualBonuses.Sum(bonus => bonus.FinalAmount),
            bonuses.Where(bonus => payableStatuses.Contains(bonus.Status)).Sum(bonus => bonus.FinalAmount),
            bonuses.Where(bonus => bonus.Status == MotivationBonusStatus.Paid).Sum(bonus => bonus.FinalAmount),
            Average(actualBonuses.Select(bonus => bonus.EfficiencyPercent)));
    }

    private static IReadOnlyList<AnalyticsDepartmentSummary> BuildDepartmentSummaries(
        IReadOnlyList<EmployeeProfile> employees,
        IReadOnlyList<KpiItem> kpis,
        IReadOnlyList<LearningAssignment> assignments,
        IReadOnlyList<MotivationBonus> bonuses)
    {
        return employees
            .GroupBy(employee => NormalizeDepartment(employee.Department), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var employeeIds = group
                    .Select(employee => employee.Id)
                    .ToHashSet();
                var departmentKpis = kpis
                    .Where(kpi => employeeIds.Contains(kpi.EmployeeId))
                    .ToList();
                var activeKpis = departmentKpis
                    .Where(kpi => kpi.Status != KpiStatus.Cancelled)
                    .ToList();
                var departmentAssignments = assignments
                    .Where(assignment => employeeIds.Contains(assignment.EmployeeId))
                    .ToList();
                var measuredAssignments = departmentAssignments
                    .Where(assignment => assignment.Status != LearningAssignmentStatus.Cancelled)
                    .ToList();
                var departmentBonuses = bonuses
                    .Where(bonus => employeeIds.Contains(bonus.EmployeeId))
                    .Where(bonus => bonus.Status is not MotivationBonusStatus.Rejected and not MotivationBonusStatus.Cancelled)
                    .ToList();

                return new AnalyticsDepartmentSummary(
                    group.Key,
                    group.Count(),
                    group.Count(employee => employee.Status == EmployeeStatus.Active),
                    departmentKpis.Count,
                    departmentKpis.Count(kpi => kpi.Status == KpiStatus.Completed),
                    departmentKpis.Count(kpi => kpi.Status == KpiStatus.Overdue),
                    Average(activeKpis.Select(kpi => kpi.CompletionPercent)),
                    departmentAssignments.Count,
                    departmentAssignments.Count(assignment => assignment.Status == LearningAssignmentStatus.Completed),
                    Percent(
                        departmentAssignments.Count(assignment => assignment.Status == LearningAssignmentStatus.Completed),
                        measuredAssignments.Count),
                    departmentBonuses.Sum(bonus => bonus.FinalAmount));
            })
            .ToList();
    }

    private static IReadOnlyList<AnalyticsEmployeeReportRow> BuildEmployeeRows(
        IReadOnlyList<EmployeeProfile> employees,
        IReadOnlyList<KpiItem> kpis,
        IReadOnlyList<LearningAssignment> assignments,
        IReadOnlyList<MotivationBonus> bonuses)
    {
        return employees
            .OrderBy(employee => NormalizeDepartment(employee.Department))
            .ThenBy(employee => employee.FullName)
            .Select(employee =>
            {
                var employeeKpis = kpis
                    .Where(kpi => kpi.EmployeeId == employee.Id)
                    .ToList();
                var activeKpis = employeeKpis
                    .Where(kpi => kpi.Status != KpiStatus.Cancelled)
                    .ToList();
                var employeeAssignments = assignments
                    .Where(assignment => assignment.EmployeeId == employee.Id)
                    .ToList();
                var measuredAssignments = employeeAssignments
                    .Where(assignment => assignment.Status != LearningAssignmentStatus.Cancelled)
                    .ToList();
                var employeeBonuses = bonuses
                    .Where(bonus => bonus.EmployeeId == employee.Id)
                    .ToList();
                var actualBonuses = employeeBonuses
                    .Where(bonus => bonus.Status is not MotivationBonusStatus.Rejected and not MotivationBonusStatus.Cancelled)
                    .ToList();

                return new AnalyticsEmployeeReportRow(
                    employee.Id,
                    employee.FullName,
                    employee.PersonnelNumber,
                    employee.Position,
                    NormalizeDepartment(employee.Department),
                    employee.Status,
                    employeeKpis.Count,
                    employeeKpis.Count(kpi => kpi.Status == KpiStatus.Completed),
                    employeeKpis.Count(kpi => kpi.Status == KpiStatus.Overdue),
                    Average(activeKpis.Select(kpi => kpi.CompletionPercent)),
                    employeeAssignments.Count,
                    employeeAssignments.Count(assignment => assignment.Status == LearningAssignmentStatus.Completed),
                    Average(measuredAssignments.Select(assignment => assignment.ProgressPercent)),
                    employeeBonuses.Count,
                    actualBonuses.Sum(bonus => bonus.FinalAmount),
                    employeeBonuses
                        .Where(bonus => bonus.Status == MotivationBonusStatus.Paid)
                        .Sum(bonus => bonus.FinalAmount));
            })
            .ToList();
    }

    private static IReadOnlyList<AnalyticsInsight> BuildInsights(
        AnalyticsEmployeeSummary employeeSummary,
        AnalyticsKpiSummary kpiSummary,
        AnalyticsLearningSummary learningSummary,
        AnalyticsMotivationSummary motivationSummary)
    {
        return
        [
            new AnalyticsInsight(
                "Персонал",
                $"{employeeSummary.ActiveEmployees}/{employeeSummary.TotalEmployees}",
                $"Активные сотрудники, отделов: {employeeSummary.DepartmentCount}"),
            new AnalyticsInsight(
                "KPI",
                $"{kpiSummary.AverageCompletionPercent:0.##}%",
                $"Выполнено: {kpiSummary.CompletedKpis}, просрочено: {kpiSummary.OverdueKpis}"),
            new AnalyticsInsight(
                "Обучение",
                $"{learningSummary.CompletionRatePercent:0.##}%",
                $"Завершено назначений: {learningSummary.CompletedAssignments}"),
            new AnalyticsInsight(
                "Мотивация",
                $"{motivationSummary.TotalPayableAmount:0.##}",
                $"К выплате, выплачено: {motivationSummary.TotalPaidAmount:0.##}")
        ];
    }

    private static string GetScopeTitle(AppUser currentUser)
    {
        return currentUser.Role switch
        {
            UserRole.Administrator => "Вся система",
            UserRole.HrSpecialist => "Все сотрудники HR-контура",
            UserRole.Manager => "Команда руководителя",
            UserRole.Employee => "Личный отчет сотрудника",
            _ => "Доступная область"
        };
    }

    private static string NormalizeDepartment(string department)
    {
        return string.IsNullOrWhiteSpace(department)
            ? "Без отдела"
            : department.Trim();
    }

    private static double Average(IEnumerable<double> values)
    {
        var list = values.ToList();

        if (list.Count == 0)
            return 0;

        return Math.Round(list.Average(), 2);
    }

    private static double Percent(int value, int total)
    {
        if (total <= 0)
            return 0;

        return Math.Round((double)value / total * 100, 2);
    }

    private async Task<AnalyticsOperationResult> AccessDeniedAsync(string details, string message)
    {
        await _auditLogService.LogAsync(
            AuditActionType.AccessDenied,
            false,
            "Отказано в доступе",
            details,
            user: _sessionService.CurrentUser);

        return AnalyticsOperationResult.Fail(message);
    }
}
