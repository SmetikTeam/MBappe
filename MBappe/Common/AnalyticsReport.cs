using MBappe.Models;
using System;
using System.Collections.Generic;

namespace MBappe.Common;

public sealed class AnalyticsReport
{
    public DateTime GeneratedAt { get; }

    public string ScopeTitle { get; }

    public AnalyticsEmployeeSummary EmployeeSummary { get; }

    public AnalyticsKpiSummary KpiSummary { get; }

    public AnalyticsLearningSummary LearningSummary { get; }

    public AnalyticsMotivationSummary MotivationSummary { get; }

    public IReadOnlyList<AnalyticsDepartmentSummary> Departments { get; }

    public IReadOnlyList<AnalyticsEmployeeReportRow> Employees { get; }

    public IReadOnlyList<AnalyticsInsight> Insights { get; }

    public AnalyticsReport(
        DateTime generatedAt,
        string scopeTitle,
        AnalyticsEmployeeSummary employeeSummary,
        AnalyticsKpiSummary kpiSummary,
        AnalyticsLearningSummary learningSummary,
        AnalyticsMotivationSummary motivationSummary,
        IReadOnlyList<AnalyticsDepartmentSummary> departments,
        IReadOnlyList<AnalyticsEmployeeReportRow> employees,
        IReadOnlyList<AnalyticsInsight> insights)
    {
        GeneratedAt = generatedAt;
        ScopeTitle = scopeTitle;
        EmployeeSummary = employeeSummary;
        KpiSummary = kpiSummary;
        LearningSummary = learningSummary;
        MotivationSummary = motivationSummary;
        Departments = departments;
        Employees = employees;
        Insights = insights;
    }
}

public sealed class AnalyticsEmployeeSummary
{
    public int TotalEmployees { get; }

    public int ActiveEmployees { get; }

    public int DismissedEmployees { get; }

    public int OnVacationEmployees { get; }

    public int SickLeaveEmployees { get; }

    public int DepartmentCount { get; }

    public int TotalUsers { get; }

    public int ActiveUsers { get; }

    public AnalyticsEmployeeSummary(
        int totalEmployees,
        int activeEmployees,
        int dismissedEmployees,
        int onVacationEmployees,
        int sickLeaveEmployees,
        int departmentCount,
        int totalUsers,
        int activeUsers)
    {
        TotalEmployees = totalEmployees;
        ActiveEmployees = activeEmployees;
        DismissedEmployees = dismissedEmployees;
        OnVacationEmployees = onVacationEmployees;
        SickLeaveEmployees = sickLeaveEmployees;
        DepartmentCount = departmentCount;
        TotalUsers = totalUsers;
        ActiveUsers = activeUsers;
    }
}

public sealed class AnalyticsKpiSummary
{
    public int TotalKpis { get; }

    public int InProgressKpis { get; }

    public int CompletedKpis { get; }

    public int OverdueKpis { get; }

    public int CancelledKpis { get; }

    public double AverageCompletionPercent { get; }

    public double CompletionRatePercent { get; }

    public AnalyticsKpiSummary(
        int totalKpis,
        int inProgressKpis,
        int completedKpis,
        int overdueKpis,
        int cancelledKpis,
        double averageCompletionPercent,
        double completionRatePercent)
    {
        TotalKpis = totalKpis;
        InProgressKpis = inProgressKpis;
        CompletedKpis = completedKpis;
        OverdueKpis = overdueKpis;
        CancelledKpis = cancelledKpis;
        AverageCompletionPercent = averageCompletionPercent;
        CompletionRatePercent = completionRatePercent;
    }
}

public sealed class AnalyticsLearningSummary
{
    public int TotalCourses { get; }

    public int ActiveCourses { get; }

    public int TotalAssignments { get; }

    public int ActiveAssignments { get; }

    public int CompletedAssignments { get; }

    public int CancelledAssignments { get; }

    public double AverageProgressPercent { get; }

    public double AverageScore { get; }

    public double CompletionRatePercent { get; }

    public AnalyticsLearningSummary(
        int totalCourses,
        int activeCourses,
        int totalAssignments,
        int activeAssignments,
        int completedAssignments,
        int cancelledAssignments,
        double averageProgressPercent,
        double averageScore,
        double completionRatePercent)
    {
        TotalCourses = totalCourses;
        ActiveCourses = activeCourses;
        TotalAssignments = totalAssignments;
        ActiveAssignments = activeAssignments;
        CompletedAssignments = completedAssignments;
        CancelledAssignments = cancelledAssignments;
        AverageProgressPercent = averageProgressPercent;
        AverageScore = averageScore;
        CompletionRatePercent = completionRatePercent;
    }
}

public sealed class AnalyticsMotivationSummary
{
    public int TotalBonuses { get; }

    public int PendingBonuses { get; }

    public int ApprovedBonuses { get; }

    public int PaidBonuses { get; }

    public int RejectedBonuses { get; }

    public int CancelledBonuses { get; }

    public decimal TotalCalculatedAmount { get; }

    public decimal TotalFinalAmount { get; }

    public decimal TotalPayableAmount { get; }

    public decimal TotalPaidAmount { get; }

    public double AverageEfficiencyPercent { get; }

    public AnalyticsMotivationSummary(
        int totalBonuses,
        int pendingBonuses,
        int approvedBonuses,
        int paidBonuses,
        int rejectedBonuses,
        int cancelledBonuses,
        decimal totalCalculatedAmount,
        decimal totalFinalAmount,
        decimal totalPayableAmount,
        decimal totalPaidAmount,
        double averageEfficiencyPercent)
    {
        TotalBonuses = totalBonuses;
        PendingBonuses = pendingBonuses;
        ApprovedBonuses = approvedBonuses;
        PaidBonuses = paidBonuses;
        RejectedBonuses = rejectedBonuses;
        CancelledBonuses = cancelledBonuses;
        TotalCalculatedAmount = totalCalculatedAmount;
        TotalFinalAmount = totalFinalAmount;
        TotalPayableAmount = totalPayableAmount;
        TotalPaidAmount = totalPaidAmount;
        AverageEfficiencyPercent = averageEfficiencyPercent;
    }
}

public sealed class AnalyticsDepartmentSummary
{
    public string Department { get; }

    public int EmployeeCount { get; }

    public int ActiveEmployeeCount { get; }

    public int TotalKpis { get; }

    public int CompletedKpis { get; }

    public int OverdueKpis { get; }

    public double AverageKpiCompletionPercent { get; }

    public int LearningAssignments { get; }

    public int CompletedLearningAssignments { get; }

    public double LearningCompletionRatePercent { get; }

    public decimal TotalBonusAmount { get; }

    public AnalyticsDepartmentSummary(
        string department,
        int employeeCount,
        int activeEmployeeCount,
        int totalKpis,
        int completedKpis,
        int overdueKpis,
        double averageKpiCompletionPercent,
        int learningAssignments,
        int completedLearningAssignments,
        double learningCompletionRatePercent,
        decimal totalBonusAmount)
    {
        Department = department;
        EmployeeCount = employeeCount;
        ActiveEmployeeCount = activeEmployeeCount;
        TotalKpis = totalKpis;
        CompletedKpis = completedKpis;
        OverdueKpis = overdueKpis;
        AverageKpiCompletionPercent = averageKpiCompletionPercent;
        LearningAssignments = learningAssignments;
        CompletedLearningAssignments = completedLearningAssignments;
        LearningCompletionRatePercent = learningCompletionRatePercent;
        TotalBonusAmount = totalBonusAmount;
    }
}

public sealed class AnalyticsEmployeeReportRow
{
    public Guid EmployeeId { get; }

    public string FullName { get; }

    public string PersonnelNumber { get; }

    public string Position { get; }

    public string Department { get; }

    public EmployeeStatus Status { get; }

    public int KpiCount { get; }

    public int CompletedKpiCount { get; }

    public int OverdueKpiCount { get; }

    public double AverageKpiCompletionPercent { get; }

    public int LearningAssignmentCount { get; }

    public int CompletedLearningAssignmentCount { get; }

    public double AverageLearningProgressPercent { get; }

    public int BonusCount { get; }

    public decimal TotalBonusAmount { get; }

    public decimal PaidBonusAmount { get; }

    public AnalyticsEmployeeReportRow(
        Guid employeeId,
        string fullName,
        string personnelNumber,
        string position,
        string department,
        EmployeeStatus status,
        int kpiCount,
        int completedKpiCount,
        int overdueKpiCount,
        double averageKpiCompletionPercent,
        int learningAssignmentCount,
        int completedLearningAssignmentCount,
        double averageLearningProgressPercent,
        int bonusCount,
        decimal totalBonusAmount,
        decimal paidBonusAmount)
    {
        EmployeeId = employeeId;
        FullName = fullName;
        PersonnelNumber = personnelNumber;
        Position = position;
        Department = department;
        Status = status;
        KpiCount = kpiCount;
        CompletedKpiCount = completedKpiCount;
        OverdueKpiCount = overdueKpiCount;
        AverageKpiCompletionPercent = averageKpiCompletionPercent;
        LearningAssignmentCount = learningAssignmentCount;
        CompletedLearningAssignmentCount = completedLearningAssignmentCount;
        AverageLearningProgressPercent = averageLearningProgressPercent;
        BonusCount = bonusCount;
        TotalBonusAmount = totalBonusAmount;
        PaidBonusAmount = paidBonusAmount;
    }
}

public sealed class AnalyticsInsight
{
    public string Title { get; }

    public string Value { get; }

    public string Caption { get; }

    public AnalyticsInsight(
        string title,
        string value,
        string caption)
    {
        Title = title;
        Value = value;
        Caption = caption;
    }
}
