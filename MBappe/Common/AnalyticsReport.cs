using MBappe.Models;
using System;
using System.Collections.Generic;

namespace MBappe.Common;

public sealed class AnalyticsSummary
{
    public DateTime PeriodStart { get; }

    public DateTime PeriodEnd { get; }

    public DateTime GeneratedAt { get; }

    public string ScopeTitle { get; }

    public int TotalEmployees { get; }

    public int ActiveEmployees { get; }

    public int DismissedEmployees { get; }

    public int OnVacationEmployees { get; }

    public int SickLeaveEmployees { get; }

    public int OnVacationOrSickLeaveEmployees { get; }

    public int DepartmentCount { get; }

    public int TotalKpis { get; }

    public int CompletedKpis { get; }

    public int InProgressKpis { get; }

    public int OverdueKpis { get; }

    public int CancelledKpis { get; }

    public double AverageKpiPercent { get; }

    public int TotalLearningAssignments { get; }

    public int CompletedLearningAssignments { get; }

    public int InProgressLearningAssignments { get; }

    public int CancelledLearningAssignments { get; }

    public double AverageLearningProgressPercent { get; }

    public int TotalBonuses { get; }

    public int PendingBonuses { get; }

    public int ApprovedBonuses { get; }

    public int RejectedBonuses { get; }

    public int PaidBonuses { get; }

    public decimal PayableBonusAmount { get; }

    public decimal PaidBonusAmount { get; }

    public AnalyticsSummary(
        DateTime periodStart,
        DateTime periodEnd,
        DateTime generatedAt,
        string scopeTitle,
        int totalEmployees,
        int activeEmployees,
        int dismissedEmployees,
        int onVacationEmployees,
        int sickLeaveEmployees,
        int departmentCount,
        int totalKpis,
        int completedKpis,
        int inProgressKpis,
        int overdueKpis,
        int cancelledKpis,
        double averageKpiPercent,
        int totalLearningAssignments,
        int completedLearningAssignments,
        int inProgressLearningAssignments,
        int cancelledLearningAssignments,
        double averageLearningProgressPercent,
        int totalBonuses,
        int pendingBonuses,
        int approvedBonuses,
        int rejectedBonuses,
        int paidBonuses,
        decimal payableBonusAmount,
        decimal paidBonusAmount)
    {
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        GeneratedAt = generatedAt;
        ScopeTitle = scopeTitle;
        TotalEmployees = totalEmployees;
        ActiveEmployees = activeEmployees;
        DismissedEmployees = dismissedEmployees;
        OnVacationEmployees = onVacationEmployees;
        SickLeaveEmployees = sickLeaveEmployees;
        OnVacationOrSickLeaveEmployees = onVacationEmployees + sickLeaveEmployees;
        DepartmentCount = departmentCount;
        TotalKpis = totalKpis;
        CompletedKpis = completedKpis;
        InProgressKpis = inProgressKpis;
        OverdueKpis = overdueKpis;
        CancelledKpis = cancelledKpis;
        AverageKpiPercent = averageKpiPercent;
        TotalLearningAssignments = totalLearningAssignments;
        CompletedLearningAssignments = completedLearningAssignments;
        InProgressLearningAssignments = inProgressLearningAssignments;
        CancelledLearningAssignments = cancelledLearningAssignments;
        AverageLearningProgressPercent = averageLearningProgressPercent;
        TotalBonuses = totalBonuses;
        PendingBonuses = pendingBonuses;
        ApprovedBonuses = approvedBonuses;
        RejectedBonuses = rejectedBonuses;
        PaidBonuses = paidBonuses;
        PayableBonusAmount = payableBonusAmount;
        PaidBonusAmount = paidBonusAmount;
    }
}

public sealed class EmployeeAnalyticsRow
{
    public Guid EmployeeId { get; }

    public string FullName { get; }

    public string PersonnelNumber { get; }

    public string Department { get; }

    public string Position { get; }

    public EmployeeStatus Status { get; }

    public double AverageKpiPercent { get; }

    public int TotalKpis { get; }

    public int OverdueKpis { get; }

    public double LearningProgressPercent { get; }

    public int TotalLearningAssignments { get; }

    public int CompletedLearningAssignments { get; }

    public decimal PayableBonusAmount { get; }

    public decimal PaidBonusAmount { get; }

    public IReadOnlyList<string> ProblemFlags { get; }

    public EmployeeAnalyticsRow(
        Guid employeeId,
        string fullName,
        string personnelNumber,
        string department,
        string position,
        EmployeeStatus status,
        double averageKpiPercent,
        int totalKpis,
        int overdueKpis,
        double learningProgressPercent,
        int totalLearningAssignments,
        int completedLearningAssignments,
        decimal payableBonusAmount,
        decimal paidBonusAmount,
        IReadOnlyList<string> problemFlags)
    {
        EmployeeId = employeeId;
        FullName = fullName;
        PersonnelNumber = personnelNumber;
        Department = department;
        Position = position;
        Status = status;
        AverageKpiPercent = averageKpiPercent;
        TotalKpis = totalKpis;
        OverdueKpis = overdueKpis;
        LearningProgressPercent = learningProgressPercent;
        TotalLearningAssignments = totalLearningAssignments;
        CompletedLearningAssignments = completedLearningAssignments;
        PayableBonusAmount = payableBonusAmount;
        PaidBonusAmount = paidBonusAmount;
        ProblemFlags = problemFlags;
    }
}

public sealed class AnalyticsReport
{
    public AnalyticsSummary Summary { get; }

    public IReadOnlyList<EmployeeAnalyticsRow> EmployeeRows { get; }

    public IReadOnlyList<string> Insights { get; }

    public AnalyticsReport(
        AnalyticsSummary summary,
        IReadOnlyList<EmployeeAnalyticsRow> employeeRows,
        IReadOnlyList<string> insights)
    {
        Summary = summary;
        EmployeeRows = employeeRows;
        Insights = insights;
    }
}
