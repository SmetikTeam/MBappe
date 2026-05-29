using MBappe.Common;
using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Analytics;

public partial class AnalyticsViewModel : ViewModelBase
{
    private readonly AnalyticsService _analyticsService;

    [ObservableProperty]
    private ObservableCollection<AnalyticsMetricCardViewModel> metricCards = [];

    [ObservableProperty]
    private ObservableCollection<AnalyticsInsightRowViewModel> insights = [];

    [ObservableProperty]
    private ObservableCollection<AnalyticsDepartmentRowViewModel> departments = [];

    [ObservableProperty]
    private ObservableCollection<AnalyticsEmployeeRowViewModel> employees = [];

    [ObservableProperty]
    private string scopeTitle = "Доступная область";

    [ObservableProperty]
    private string generatedAtText = "-";

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasReport;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasNoReport => !HasReport && !IsBusy;

    public AnalyticsViewModel(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;

        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        var result = await _analyticsService.GetReportAsync();

        IsBusy = false;

        if (!result.Success || result.Report is null)
        {
            ClearReport();
            StatusMessage = result.Message;
            return;
        }

        ApplyReport(result.Report);
        StatusMessage = result.Message;
    }

    private void ApplyReport(AnalyticsReport report)
    {
        ScopeTitle = report.ScopeTitle;
        GeneratedAtText = report.GeneratedAt.ToString("dd.MM.yyyy HH:mm");

        MetricCards =
        [
            new AnalyticsMetricCardViewModel(
                "Сотрудники",
                $"{report.EmployeeSummary.ActiveEmployees}/{report.EmployeeSummary.TotalEmployees}",
                $"Активные сотрудники, отделов: {report.EmployeeSummary.DepartmentCount}"),
            new AnalyticsMetricCardViewModel(
                "KPI",
                FormatPercent(report.KpiSummary.AverageCompletionPercent),
                $"Выполнено: {report.KpiSummary.CompletedKpis}, просрочено: {report.KpiSummary.OverdueKpis}"),
            new AnalyticsMetricCardViewModel(
                "Обучение",
                FormatPercent(report.LearningSummary.CompletionRatePercent),
                $"Курсов: {report.LearningSummary.ActiveCourses}/{report.LearningSummary.TotalCourses}, средний балл: {FormatNumber(report.LearningSummary.AverageScore)}"),
            new AnalyticsMetricCardViewModel(
                "Мотивация",
                FormatMoney(report.MotivationSummary.TotalPayableAmount),
                $"К выплате, выплачено: {FormatMoney(report.MotivationSummary.TotalPaidAmount)}")
        ];

        Insights = new ObservableCollection<AnalyticsInsightRowViewModel>(
            report.Insights.Select(insight => new AnalyticsInsightRowViewModel(insight)));

        Departments = new ObservableCollection<AnalyticsDepartmentRowViewModel>(
            report.Departments.Select(department => new AnalyticsDepartmentRowViewModel(department)));

        Employees = new ObservableCollection<AnalyticsEmployeeRowViewModel>(
            report.Employees.Select(employee => new AnalyticsEmployeeRowViewModel(employee)));

        HasReport = true;
    }

    private void ClearReport()
    {
        MetricCards = [];
        Insights = [];
        Departments = [];
        Employees = [];
        ScopeTitle = "Доступная область";
        GeneratedAtText = "-";
        HasReport = false;
    }

    private static string FormatPercent(double value)
    {
        return $"{value:0.##}%";
    }

    private static string FormatNumber(double value)
    {
        return value == 0 ? "0" : $"{value:0.##}";
    }

    private static string FormatMoney(decimal value)
    {
        return value == 0 ? "0" : $"{value:0.##}";
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoReport));
    }

    partial void OnHasReportChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoReport));
    }
}

public sealed class AnalyticsMetricCardViewModel
{
    public string Title { get; }

    public string Value { get; }

    public string Caption { get; }

    public AnalyticsMetricCardViewModel(
        string title,
        string value,
        string caption)
    {
        Title = title;
        Value = value;
        Caption = caption;
    }
}

public sealed class AnalyticsInsightRowViewModel
{
    public string Title { get; }

    public string Value { get; }

    public string Caption { get; }

    public AnalyticsInsightRowViewModel(AnalyticsInsight insight)
    {
        Title = insight.Title;
        Value = insight.Value;
        Caption = insight.Caption;
    }
}

public sealed class AnalyticsDepartmentRowViewModel
{
    public string Department { get; }

    public string EmployeesText { get; }

    public string KpiText { get; }

    public string AverageKpiCompletionText { get; }

    public string LearningText { get; }

    public string LearningCompletionRateText { get; }

    public string BonusText { get; }

    public AnalyticsDepartmentRowViewModel(AnalyticsDepartmentSummary summary)
    {
        Department = summary.Department;
        EmployeesText = $"{summary.ActiveEmployeeCount}/{summary.EmployeeCount}";
        KpiText = $"{summary.CompletedKpis}/{summary.TotalKpis}";
        AverageKpiCompletionText = FormatPercent(summary.AverageKpiCompletionPercent);
        LearningText = $"{summary.CompletedLearningAssignments}/{summary.LearningAssignments}";
        LearningCompletionRateText = FormatPercent(summary.LearningCompletionRatePercent);
        BonusText = FormatMoney(summary.TotalBonusAmount);
    }

    private static string FormatPercent(double value)
    {
        return $"{value:0.##}%";
    }

    private static string FormatMoney(decimal value)
    {
        return value == 0 ? "0" : $"{value:0.##}";
    }
}

public sealed class AnalyticsEmployeeRowViewModel
{
    public string FullName { get; }

    public string PersonnelNumber { get; }

    public string Position { get; }

    public string Department { get; }

    public string StatusTitle { get; }

    public string KpiText { get; }

    public string KpiAverageText { get; }

    public string LearningText { get; }

    public string LearningAverageText { get; }

    public string BonusText { get; }

    public string PaidBonusText { get; }

    public AnalyticsEmployeeRowViewModel(AnalyticsEmployeeReportRow row)
    {
        FullName = row.FullName;
        PersonnelNumber = row.PersonnelNumber;
        Position = row.Position;
        Department = row.Department;
        StatusTitle = DisplayNames.ForEmployeeStatus(row.Status);
        KpiText = $"{row.CompletedKpiCount}/{row.KpiCount}";
        KpiAverageText = FormatPercent(row.AverageKpiCompletionPercent);
        LearningText = $"{row.CompletedLearningAssignmentCount}/{row.LearningAssignmentCount}";
        LearningAverageText = FormatPercent(row.AverageLearningProgressPercent);
        BonusText = FormatMoney(row.TotalBonusAmount);
        PaidBonusText = FormatMoney(row.PaidBonusAmount);
    }

    private static string FormatPercent(double value)
    {
        return $"{value:0.##}%";
    }

    private static string FormatMoney(decimal value)
    {
        return value == 0 ? "0" : $"{value:0.##}";
    }
}
