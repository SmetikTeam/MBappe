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
    private readonly IKpiRepository _kpiRepository;
    private readonly ILearningRepository _learningRepository;
    private readonly IMotivationBonusRepository _bonusRepository;
    private readonly SessionService _sessionService;
    private readonly AuditLogService _auditLogService;

    public AnalyticsService(
        IEmployeeRepository employeeRepository,
        IKpiRepository kpiRepository,
        ILearningRepository learningRepository,
        IMotivationBonusRepository bonusRepository,
        SessionService sessionService,
        AuditLogService auditLogService)
    {
        _employeeRepository = employeeRepository;
        _kpiRepository = kpiRepository;
        _learningRepository = learningRepository;
        _bonusRepository = bonusRepository;
        _sessionService = sessionService;
        _auditLogService = auditLogService;
    }

    public Task<AnalyticsOperationResult> GetReportAsync()
    {
        return GetDashboardReportAsync(DateTime.Today.AddMonths(-1), DateTime.Today);
    }

    public async Task<AnalyticsOperationResult> GetDashboardReportAsync(DateTime periodStart, DateTime periodEnd)
    {
        periodStart = periodStart.Date;
        periodEnd = periodEnd.Date;

        if (periodEnd < periodStart)
            return AnalyticsOperationResult.Fail("Дата окончания периода не может быть раньше даты начала");

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

        var kpis = (await _kpiRepository.GetAllAsync())
            .Where(kpi => visibleEmployeeIds.Contains(kpi.EmployeeId))
            .Where(kpi => DateRangesIntersect(kpi.PeriodStart, kpi.PeriodEnd, periodStart, periodEnd))
            .ToList();

        var assignments = (await _learningRepository.GetAllAssignmentsAsync())
            .Where(assignment => visibleEmployeeIds.Contains(assignment.EmployeeId))
            .Where(assignment => AssignmentIntersectsPeriod(assignment, periodStart, periodEnd))
            .ToList();

        var bonuses = (await _bonusRepository.GetAllAsync())
            .Where(bonus => visibleEmployeeIds.Contains(bonus.EmployeeId))
            .Where(bonus => DateRangesIntersect(bonus.PeriodStart, bonus.PeriodEnd, periodStart, periodEnd))
            .ToList();

        var summary = BuildSummary(
            periodStart,
            periodEnd,
            DateTime.Now,
            GetScopeTitle(currentUser),
            visibleEmployees,
            kpis,
            assignments,
            bonuses);

        var employeeRows = BuildEmployeeRows(visibleEmployees, kpis, assignments, bonuses);
        var insights = BuildInsights(summary, employeeRows);
        var report = new AnalyticsReport(summary, employeeRows, insights);

        await _auditLogService.LogAsync(
            AuditActionType.AnalyticsReportGenerated,
            true,
            "Сформирован аналитический отчет",
            $"Период: {periodStart:dd.MM.yyyy}-{periodEnd:dd.MM.yyyy}; область: {summary.ScopeTitle}; сотрудников: {summary.TotalEmployees}",
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

    private static AnalyticsSummary BuildSummary(
        DateTime periodStart,
        DateTime periodEnd,
        DateTime generatedAt,
        string scopeTitle,
        IReadOnlyList<EmployeeProfile> employees,
        IReadOnlyList<KpiItem> kpis,
        IReadOnlyList<LearningAssignment> assignments,
        IReadOnlyList<MotivationBonus> bonuses)
    {
        var measuredKpis = kpis
            .Where(kpi => kpi.Status != KpiStatus.Cancelled)
            .ToList();
        var measuredAssignments = assignments
            .Where(assignment => assignment.Status != LearningAssignmentStatus.Cancelled)
            .ToList();

        return new AnalyticsSummary(
            periodStart,
            periodEnd,
            generatedAt,
            scopeTitle,
            employees.Count,
            employees.Count(employee => employee.Status == EmployeeStatus.Active),
            employees.Count(employee => employee.Status == EmployeeStatus.Dismissed),
            employees.Count(employee => employee.Status == EmployeeStatus.OnVacation),
            employees.Count(employee => employee.Status == EmployeeStatus.SickLeave),
            CountDepartments(employees),
            kpis.Count,
            kpis.Count(kpi => kpi.Status == KpiStatus.Completed),
            kpis.Count(kpi => kpi.Status == KpiStatus.InProgress),
            kpis.Count(kpi => kpi.Status == KpiStatus.Overdue),
            kpis.Count(kpi => kpi.Status == KpiStatus.Cancelled),
            Average(measuredKpis.Select(kpi => kpi.CompletionPercent)),
            assignments.Count,
            assignments.Count(assignment => assignment.Status == LearningAssignmentStatus.Completed),
            assignments.Count(assignment => assignment.Status is LearningAssignmentStatus.Assigned or LearningAssignmentStatus.InProgress),
            assignments.Count(assignment => assignment.Status == LearningAssignmentStatus.Cancelled),
            Average(measuredAssignments.Select(assignment => assignment.ProgressPercent)),
            bonuses.Count,
            bonuses.Count(bonus => bonus.Status == MotivationBonusStatus.PendingApproval),
            bonuses.Count(bonus => bonus.Status == MotivationBonusStatus.Approved),
            bonuses.Count(bonus => bonus.Status == MotivationBonusStatus.Rejected),
            bonuses.Count(bonus => bonus.Status == MotivationBonusStatus.Paid),
            bonuses
                .Where(bonus => bonus.Status is MotivationBonusStatus.PendingApproval or MotivationBonusStatus.Approved)
                .Sum(bonus => bonus.FinalAmount),
            bonuses
                .Where(bonus => bonus.Status == MotivationBonusStatus.Paid)
                .Sum(bonus => bonus.FinalAmount));
    }

    private static IReadOnlyList<EmployeeAnalyticsRow> BuildEmployeeRows(
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
                var measuredKpis = employeeKpis
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

                var averageKpiPercent = Average(measuredKpis.Select(kpi => kpi.CompletionPercent));
                var learningProgressPercent = Average(measuredAssignments.Select(assignment => assignment.ProgressPercent));
                var payableBonusAmount = employeeBonuses
                    .Where(bonus => bonus.Status is MotivationBonusStatus.PendingApproval or MotivationBonusStatus.Approved)
                    .Sum(bonus => bonus.FinalAmount);
                var paidBonusAmount = employeeBonuses
                    .Where(bonus => bonus.Status == MotivationBonusStatus.Paid)
                    .Sum(bonus => bonus.FinalAmount);
                var overdueKpis = employeeKpis.Count(kpi => kpi.Status == KpiStatus.Overdue);
                var problemFlags = BuildProblemFlags(
                    averageKpiPercent,
                    measuredKpis.Count,
                    overdueKpis,
                    learningProgressPercent,
                    measuredAssignments.Count,
                    employeeBonuses.Any(bonus => bonus.Status == MotivationBonusStatus.PendingApproval));

                return new EmployeeAnalyticsRow(
                    employee.Id,
                    employee.FullName,
                    employee.PersonnelNumber,
                    NormalizeDepartment(employee.Department),
                    employee.Position,
                    employee.Status,
                    averageKpiPercent,
                    employeeKpis.Count,
                    overdueKpis,
                    learningProgressPercent,
                    employeeAssignments.Count,
                    employeeAssignments.Count(assignment => assignment.Status == LearningAssignmentStatus.Completed),
                    payableBonusAmount,
                    paidBonusAmount,
                    problemFlags);
            })
            .ToList();
    }

    private static IReadOnlyList<string> BuildProblemFlags(
        double averageKpiPercent,
        int measuredKpiCount,
        int overdueKpis,
        double learningProgressPercent,
        int measuredLearningAssignmentCount,
        bool hasPendingBonus)
    {
        var flags = new List<string>();

        if (measuredKpiCount > 0 && averageKpiPercent < 70)
            flags.Add("Низкий KPI");

        if (overdueKpis > 0)
            flags.Add("Есть просроченные KPI");

        if (measuredLearningAssignmentCount > 0 && learningProgressPercent < 50)
            flags.Add("Низкий прогресс обучения");

        if (hasPendingBonus)
            flags.Add("Есть бонусы на утверждении");

        return flags.Count == 0 ? ["Без замечаний"] : flags;
    }

    private static IReadOnlyList<string> BuildInsights(
        AnalyticsSummary summary,
        IReadOnlyList<EmployeeAnalyticsRow> employeeRows)
    {
        var problemEmployeeCount = employeeRows.Count(row =>
            row.ProblemFlags.Any(flag => flag != "Без замечаний"));

        return
        [
            $"Период отчета: {summary.PeriodStart:dd.MM.yyyy} - {summary.PeriodEnd:dd.MM.yyyy}.",
            $"В области отчета сотрудников: {summary.TotalEmployees}, активных: {summary.ActiveEmployees}.",
            $"Средний KPI: {summary.AverageKpiPercent:0.##}%, просроченных KPI: {summary.OverdueKpis}.",
            $"Средний прогресс обучения: {summary.AverageLearningProgressPercent:0.##}%, завершено назначений: {summary.CompletedLearningAssignments}.",
            $"Сумма бонусов к выплате: {summary.PayableBonusAmount:0.##}, уже выплачено: {summary.PaidBonusAmount:0.##}.",
            $"Сотрудников с замечаниями: {problemEmployeeCount}."
        ];
    }

    private static int CountDepartments(IReadOnlyList<EmployeeProfile> employees)
    {
        return employees
            .Select(employee => NormalizeDepartment(employee.Department))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    private static string GetScopeTitle(AppUser currentUser)
    {
        return currentUser.Role switch
        {
            UserRole.Administrator => "Все сотрудники",
            UserRole.HrSpecialist => "Все сотрудники",
            UserRole.Manager => "Руководитель и подчиненные",
            UserRole.Employee => "Личная аналитика",
            _ => "Доступная область"
        };
    }

    private static bool DateRangesIntersect(
        DateTime itemStart,
        DateTime itemEnd,
        DateTime periodStart,
        DateTime periodEnd)
    {
        itemStart = itemStart.Date;
        itemEnd = itemEnd.Date;

        if (itemEnd < itemStart)
            (itemStart, itemEnd) = (itemEnd, itemStart);

        return itemStart <= periodEnd && itemEnd >= periodStart;
    }

    private static bool AssignmentIntersectsPeriod(
        LearningAssignment assignment,
        DateTime periodStart,
        DateTime periodEnd)
    {
        var assignmentStart = assignment.AssignedAt.Date;
        var assignmentEnd = assignment.CompletedAt?.Date ?? assignment.DueDate?.Date;

        if (assignmentEnd is null)
            return assignmentStart <= periodEnd;

        return DateRangesIntersect(assignmentStart, assignmentEnd.Value, periodStart, periodEnd);
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
