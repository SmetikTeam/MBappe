using MBappe.Common;
using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Analytics;

public partial class AnalyticsViewModel : ViewModelBase
{
    private static readonly string[] SupportedDateFormats = ["dd.MM.yyyy", "yyyy-MM-dd"];
    private readonly AnalyticsService _analyticsService;

    [ObservableProperty]
    private AnalyticsSummary? summary;

    [ObservableProperty]
    private ObservableCollection<EmployeeAnalyticsRowViewModel> employeeRows = [];

    [ObservableProperty]
    private ObservableCollection<string> insights = [];

    [ObservableProperty]
    private string periodStartText = string.Empty;

    [ObservableProperty]
    private string periodEndText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public bool HasSummary => Summary is not null;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasNoReport => !HasSummary && !IsBusy;

    public string ScopeText => Summary?.ScopeTitle ?? "Доступная область";

    public string GeneratedAtText => Summary is null ? "-" : Summary.GeneratedAt.ToString("dd.MM.yyyy HH:mm");

    public string ActiveEmployeesText => Summary is null ? "0" : $"{Summary.ActiveEmployees}/{Summary.TotalEmployees}";

    public string AverageKpiText => FormatPercent(Summary?.AverageKpiPercent ?? 0);

    public string LearningProgressText => FormatPercent(Summary?.AverageLearningProgressPercent ?? 0);

    public string PayableBonusText => FormatMoney(Summary?.PayableBonusAmount ?? 0);

    public string PersonnelPrimaryText => Summary is null
        ? "0 сотрудников"
        : $"{Summary.ActiveEmployees} активных из {Summary.TotalEmployees}";

    public string PersonnelSecondaryText => Summary is null
        ? "Уволены: 0, отпуск/больничный: 0, отделов: 0"
        : $"Уволены: {Summary.DismissedEmployees}, отпуск/больничный: {Summary.OnVacationOrSickLeaveEmployees}, отделов: {Summary.DepartmentCount}";

    public string KpiPrimaryText => Summary is null
        ? "0 KPI"
        : $"{Summary.CompletedKpis}/{Summary.TotalKpis} выполнено";

    public string KpiSecondaryText => Summary is null
        ? "В работе: 0, просрочено: 0, отменено: 0"
        : $"В работе: {Summary.InProgressKpis}, просрочено: {Summary.OverdueKpis}, отменено: {Summary.CancelledKpis}";

    public string LearningPrimaryText => Summary is null
        ? "0 назначений"
        : $"{Summary.CompletedLearningAssignments}/{Summary.TotalLearningAssignments} завершено";

    public string LearningSecondaryText => Summary is null
        ? "В процессе: 0, отменено: 0"
        : $"В процессе: {Summary.InProgressLearningAssignments}, отменено: {Summary.CancelledLearningAssignments}";

    public string MotivationPrimaryText => Summary is null
        ? "0 бонусов"
        : $"{Summary.TotalBonuses} бонусов";

    public string MotivationSecondaryText => Summary is null
        ? "На утверждении: 0, утверждено: 0, отклонено: 0, выплачено: 0"
        : $"На утверждении: {Summary.PendingBonuses}, утверждено: {Summary.ApprovedBonuses}, отклонено: {Summary.RejectedBonuses}, выплачено: {Summary.PaidBonuses}";

    public AnalyticsViewModel(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;

        var periodEnd = DateTime.Today;
        var periodStart = periodEnd.AddMonths(-1);
        PeriodStartText = periodStart.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        PeriodEndText = periodEnd.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        _ = GenerateReportAsync();
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        if (IsBusy)
            return;

        if (!TryParsePeriod(out var periodStart, out var periodEnd))
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _analyticsService.GetDashboardReportAsync(periodStart, periodEnd);

            if (!result.Success || result.Report is null)
            {
                ClearReport();
                StatusMessage = result.Message;
                return;
            }

            ApplyReport(result.Report);
            StatusMessage = result.Message;
        }
        catch (Exception exception)
        {
            ClearReport();
            StatusMessage = $"Не удалось сформировать отчет: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryParsePeriod(out DateTime periodStart, out DateTime periodEnd)
    {
        if (!TryParseDate(PeriodStartText, out periodStart))
        {
            periodEnd = default;
            StatusMessage = "Введите начало периода в формате dd.MM.yyyy или yyyy-MM-dd";
            return false;
        }

        if (!TryParseDate(PeriodEndText, out periodEnd))
        {
            StatusMessage = "Введите конец периода в формате dd.MM.yyyy или yyyy-MM-dd";
            return false;
        }

        if (periodEnd.Date < periodStart.Date)
        {
            StatusMessage = "Дата окончания периода не может быть раньше даты начала";
            return false;
        }

        periodStart = periodStart.Date;
        periodEnd = periodEnd.Date;
        return true;
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        return DateTime.TryParseExact(
            value.Trim(),
            SupportedDateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    private void ApplyReport(AnalyticsReport report)
    {
        Summary = report.Summary;
        EmployeeRows = new ObservableCollection<EmployeeAnalyticsRowViewModel>(
            report.EmployeeRows.Select(row => new EmployeeAnalyticsRowViewModel(row)));
        Insights = new ObservableCollection<string>(report.Insights);
    }

    private void ClearReport()
    {
        Summary = null;
        EmployeeRows = [];
        Insights = [];
    }

    private static string FormatPercent(double value)
    {
        return $"{value:0.##}%";
    }

    private static string FormatMoney(decimal value)
    {
        return value == 0 ? "0" : $"{value:0.##}";
    }

    partial void OnSummaryChanged(AnalyticsSummary? value)
    {
        OnPropertyChanged(nameof(HasSummary));
        OnPropertyChanged(nameof(HasNoReport));
        OnPropertyChanged(nameof(ScopeText));
        OnPropertyChanged(nameof(GeneratedAtText));
        OnPropertyChanged(nameof(ActiveEmployeesText));
        OnPropertyChanged(nameof(AverageKpiText));
        OnPropertyChanged(nameof(LearningProgressText));
        OnPropertyChanged(nameof(PayableBonusText));
        OnPropertyChanged(nameof(PersonnelPrimaryText));
        OnPropertyChanged(nameof(PersonnelSecondaryText));
        OnPropertyChanged(nameof(KpiPrimaryText));
        OnPropertyChanged(nameof(KpiSecondaryText));
        OnPropertyChanged(nameof(LearningPrimaryText));
        OnPropertyChanged(nameof(LearningSecondaryText));
        OnPropertyChanged(nameof(MotivationPrimaryText));
        OnPropertyChanged(nameof(MotivationSecondaryText));
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoReport));
    }
}

public sealed class EmployeeAnalyticsRowViewModel
{
    public string FullName { get; }

    public string PersonnelNumber { get; }

    public string Department { get; }

    public string Position { get; }

    public string StatusTitle { get; }

    public string AverageKpiText { get; }

    public string KpiDetailsText { get; }

    public string LearningProgressText { get; }

    public string LearningDetailsText { get; }

    public string PayableBonusText { get; }

    public string PaidBonusText { get; }

    public string ProblemFlagsText { get; }

    public EmployeeAnalyticsRowViewModel(EmployeeAnalyticsRow row)
    {
        FullName = row.FullName;
        PersonnelNumber = row.PersonnelNumber;
        Department = row.Department;
        Position = row.Position;
        StatusTitle = DisplayNames.ForEmployeeStatus(row.Status);
        AverageKpiText = FormatPercent(row.AverageKpiPercent);
        KpiDetailsText = $"Всего: {row.TotalKpis}, просрочено: {row.OverdueKpis}";
        LearningProgressText = FormatPercent(row.LearningProgressPercent);
        LearningDetailsText = $"{row.CompletedLearningAssignments}/{row.TotalLearningAssignments} завершено";
        PayableBonusText = FormatMoney(row.PayableBonusAmount);
        PaidBonusText = $"Выплачено: {FormatMoney(row.PaidBonusAmount)}";
        ProblemFlagsText = string.Join(", ", row.ProblemFlags);
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
