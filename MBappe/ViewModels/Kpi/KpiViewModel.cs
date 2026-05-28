using MBappe.Common;
using MBappe.Models;
using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Kpi;

public partial class KpiViewModel : ViewModelBase
{
    private readonly KpiService _kpiService;
    private readonly EmployeeService _employeeService;
    private IReadOnlyDictionary<Guid, EmployeeProfile> _employeesById = new Dictionary<Guid, EmployeeProfile>();

    [ObservableProperty]
    private ObservableCollection<KpiRowViewModel> kpis = [];

    [ObservableProperty]
    private ObservableCollection<KpiEmployeeOption> employeeOptions = [];

    [ObservableProperty]
    private KpiRowViewModel? selectedKpi;

    [ObservableProperty]
    private KpiEmployeeOption? selectedEmployeeOption;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string createFormMessage = string.Empty;

    [ObservableProperty]
    private string editFormMessage = string.Empty;

    [ObservableProperty]
    private string progressFormMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isCreateFormVisible;

    [ObservableProperty]
    private bool isEditFormVisible;

    [ObservableProperty]
    private bool isProgressFormVisible;

    [ObservableProperty]
    private string newTitle = string.Empty;

    [ObservableProperty]
    private string newDescription = string.Empty;

    [ObservableProperty]
    private string newTargetValueText = string.Empty;

    [ObservableProperty]
    private string newActualValueText = "0";

    [ObservableProperty]
    private string newUnit = string.Empty;

    [ObservableProperty]
    private string newWeightPercentText = "100";

    [ObservableProperty]
    private string newPeriodStartText = DateTime.Today.ToString("dd.MM.yyyy");

    [ObservableProperty]
    private string newPeriodEndText = DateTime.Today.AddMonths(1).ToString("dd.MM.yyyy");

    [ObservableProperty]
    private string editTitle = string.Empty;

    [ObservableProperty]
    private string editDescription = string.Empty;

    [ObservableProperty]
    private string editTargetValueText = string.Empty;

    [ObservableProperty]
    private string editUnit = string.Empty;

    [ObservableProperty]
    private string editWeightPercentText = string.Empty;

    [ObservableProperty]
    private string editPeriodStartText = string.Empty;

    [ObservableProperty]
    private string editPeriodEndText = string.Empty;

    [ObservableProperty]
    private string progressActualValueText = string.Empty;

    [ObservableProperty]
    private int totalKpiCount;

    [ObservableProperty]
    private int completedKpiCount;

    [ObservableProperty]
    private int overdueKpiCount;

    [ObservableProperty]
    private string averageCompletionText = "0%";

    public bool CanManageKpis => _kpiService.CanManageKpis();

    public bool IsReadOnlyAccess => !CanManageKpis;

    public bool HasSelectedKpi => SelectedKpi is not null;

    public bool HasNoSelectedKpi => SelectedKpi is null;

    public bool CanShowKpiActions => CanManageKpis && HasSelectedKpi;

    public bool CanShowActionPrompt => CanManageKpis && HasNoSelectedKpi;

    public bool CanShowKpiDetails =>
        HasSelectedKpi
        && !IsCreateFormVisible
        && !IsEditFormVisible
        && !IsProgressFormVisible;

    public string SelectedKpiCaption => SelectedKpi is null
        ? "KPI не выбран"
        : $"{SelectedKpi.Title} · {SelectedKpi.EmployeeTitle}";

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasCreateFormMessage => !string.IsNullOrWhiteSpace(CreateFormMessage);

    public bool HasEditFormMessage => !string.IsNullOrWhiteSpace(EditFormMessage);

    public bool HasProgressFormMessage => !string.IsNullOrWhiteSpace(ProgressFormMessage);

    public KpiViewModel(KpiService kpiService, EmployeeService employeeService)
    {
        _kpiService = kpiService;
        _employeeService = employeeService;

        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        HideFormsAfterSelectionChanged();

        IsBusy = true;
        var selectedKpiId = SelectedKpi?.Kpi.Id;

        await LoadEmployeesAsync();

        var result = await _kpiService.GetVisibleKpisAsync();

        IsBusy = false;

        if (!result.Success || result.Kpis is null)
        {
            StatusMessage = result.Message;
            return;
        }

        Kpis = new ObservableCollection<KpiRowViewModel>(
            result.Kpis.Select(kpi =>
            {
                _employeesById.TryGetValue(kpi.EmployeeId, out var employee);
                return new KpiRowViewModel(kpi, employee);
            }));

        SelectedKpi = selectedKpiId is null
            ? Kpis.FirstOrDefault()
            : Kpis.FirstOrDefault(kpi => kpi.Kpi.Id == selectedKpiId) ?? Kpis.FirstOrDefault();

        UpdateSummary();

        StatusMessage = result.Message;
    }

    [RelayCommand]
    private void OpenCreateForm()
    {
        if (!CanManageKpis)
            return;

        ClearMessages();
        ClearCreateForm();

        IsCreateFormVisible = true;
        IsEditFormVisible = false;
        IsProgressFormVisible = false;
    }

    [RelayCommand]
    private void OpenEditForm()
    {
        if (!CanManageKpis || SelectedKpi is null)
            return;

        ClearMessages();

        EditTitle = SelectedKpi.Kpi.Title;
        EditDescription = SelectedKpi.Kpi.Description;
        EditTargetValueText = FormatNumber(SelectedKpi.Kpi.TargetValue);
        EditUnit = SelectedKpi.Kpi.Unit;
        EditWeightPercentText = FormatNumber(SelectedKpi.Kpi.WeightPercent);
        EditPeriodStartText = SelectedKpi.Kpi.PeriodStart.ToString("dd.MM.yyyy");
        EditPeriodEndText = SelectedKpi.Kpi.PeriodEnd.ToString("dd.MM.yyyy");

        IsCreateFormVisible = false;
        IsEditFormVisible = true;
        IsProgressFormVisible = false;
    }

    [RelayCommand]
    private void OpenProgressForm()
    {
        if (!CanManageKpis || SelectedKpi is null)
            return;

        ClearMessages();

        ProgressActualValueText = FormatNumber(SelectedKpi.Kpi.ActualValue);

        IsCreateFormVisible = false;
        IsEditFormVisible = false;
        IsProgressFormVisible = true;
    }

    [RelayCommand]
    private void CloseForms()
    {
        IsCreateFormVisible = false;
        IsEditFormVisible = false;
        IsProgressFormVisible = false;
        ClearMessages();
    }

    private void HideFormsAfterSelectionChanged()
    {
        IsCreateFormVisible = false;
        IsEditFormVisible = false;
        IsProgressFormVisible = false;

        CreateFormMessage = string.Empty;
        EditFormMessage = string.Empty;
        ProgressFormMessage = string.Empty;
    }

    [RelayCommand]
    private async Task CreateKpiAsync()
    {
        if (!CanManageKpis)
            return;

        if (SelectedEmployeeOption is null)
        {
            CreateFormMessage = "Выберите сотрудника";
            return;
        }

        if (!TryBuildCreateRequest(out var request, out var error))
        {
            CreateFormMessage = error;
            return;
        }

        var result = await _kpiService.CreateKpiAsync(request);

        if (!result.Success)
        {
            CreateFormMessage = result.Message;
            return;
        }

        await RefreshAsync();

        IsCreateFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task SaveKpiAsync()
    {
        if (!CanManageKpis)
            return;

        if (SelectedKpi is null)
        {
            EditFormMessage = "Выберите KPI";
            return;
        }

        if (!TryBuildUpdateRequest(SelectedKpi.Kpi.Id, out var request, out var error))
        {
            EditFormMessage = error;
            return;
        }

        var selectedKpiId = SelectedKpi.Kpi.Id;
        var result = await _kpiService.UpdateKpiAsync(request);

        if (!result.Success)
        {
            EditFormMessage = result.Message;
            return;
        }

        await RefreshAsync();

        SelectedKpi = Kpis.FirstOrDefault(kpi => kpi.Kpi.Id == selectedKpiId);
        IsEditFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task SaveProgressAsync()
    {
        if (!CanManageKpis)
            return;

        if (SelectedKpi is null)
        {
            ProgressFormMessage = "Выберите KPI";
            return;
        }

        if (!TryParseDouble(ProgressActualValueText, out var actualValue))
        {
            ProgressFormMessage = "Введите фактическое значение числом";
            return;
        }

        var selectedKpiId = SelectedKpi.Kpi.Id;

        var result = await _kpiService.UpdateKpiProgressAsync(new UpdateKpiProgressRequest
        {
            KpiId = selectedKpiId,
            ActualValue = actualValue
        });

        if (!result.Success)
        {
            ProgressFormMessage = result.Message;
            return;
        }

        await RefreshAsync();

        SelectedKpi = Kpis.FirstOrDefault(kpi => kpi.Kpi.Id == selectedKpiId);
        IsProgressFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task CancelSelectedKpiAsync()
    {
        if (!CanManageKpis)
            return;

        if (SelectedKpi is null)
        {
            StatusMessage = "Выберите KPI";
            return;
        }

        var selectedKpiId = SelectedKpi.Kpi.Id;
        var result = await _kpiService.CancelKpiAsync(selectedKpiId);

        StatusMessage = result.Message;

        if (!result.Success)
            return;

        await RefreshAsync();
        SelectedKpi = Kpis.FirstOrDefault(kpi => kpi.Kpi.Id == selectedKpiId);
        StatusMessage = result.Message;
    }

    private async Task LoadEmployeesAsync()
    {
        EmployeeOptions = [];

        var employeesResult = CanManageKpis
            ? await _employeeService.GetAllEmployeesAsync()
            : await _employeeService.GetCurrentEmployeeProfileAsync();

        IReadOnlyList<EmployeeProfile> employees;

        if (employeesResult.Employees is not null)
        {
            employees = employeesResult.Employees;
        }
        else if (employeesResult.Employee is not null)
        {
            employees = [employeesResult.Employee];
        }
        else
        {
            _employeesById = new Dictionary<Guid, EmployeeProfile>();
            return;
        }

        _employeesById = employees.ToDictionary(employee => employee.Id);

        EmployeeOptions = new ObservableCollection<KpiEmployeeOption>(
            employees
                .Where(employee => employee.Status != EmployeeStatus.Dismissed)
                .OrderBy(employee => employee.FullName)
                .Select(employee => new KpiEmployeeOption(employee)));

        SelectedEmployeeOption = EmployeeOptions.FirstOrDefault();
    }

    private bool TryBuildCreateRequest(out CreateKpiRequest request, out string error)
    {
        request = new CreateKpiRequest();
        error = string.Empty;

        if (SelectedEmployeeOption is null)
        {
            error = "Выберите сотрудника";
            return false;
        }

        if (!TryParseDouble(NewTargetValueText, out var targetValue))
        {
            error = "Введите плановое значение числом";
            return false;
        }

        if (!TryParseDouble(NewActualValueText, out var actualValue))
        {
            error = "Введите фактическое значение числом";
            return false;
        }

        if (!TryParseDouble(NewWeightPercentText, out var weightPercent))
        {
            error = "Введите вес KPI числом";
            return false;
        }

        if (!TryParseDate(NewPeriodStartText, out var periodStart))
        {
            error = "Введите дату начала в формате дд.мм.гггг";
            return false;
        }

        if (!TryParseDate(NewPeriodEndText, out var periodEnd))
        {
            error = "Введите дату окончания в формате дд.мм.гггг";
            return false;
        }

        request = new CreateKpiRequest
        {
            EmployeeId = SelectedEmployeeOption.Employee.Id,
            Title = NewTitle,
            Description = NewDescription,
            TargetValue = targetValue,
            ActualValue = actualValue,
            Unit = NewUnit,
            WeightPercent = weightPercent,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        };

        return true;
    }

    private bool TryBuildUpdateRequest(Guid kpiId, out UpdateKpiRequest request, out string error)
    {
        request = new UpdateKpiRequest();
        error = string.Empty;

        if (!TryParseDouble(EditTargetValueText, out var targetValue))
        {
            error = "Введите плановое значение числом";
            return false;
        }

        if (!TryParseDouble(EditWeightPercentText, out var weightPercent))
        {
            error = "Введите вес KPI числом";
            return false;
        }

        if (!TryParseDate(EditPeriodStartText, out var periodStart))
        {
            error = "Введите дату начала в формате дд.мм.гггг";
            return false;
        }

        if (!TryParseDate(EditPeriodEndText, out var periodEnd))
        {
            error = "Введите дату окончания в формате дд.мм.гггг";
            return false;
        }

        request = new UpdateKpiRequest
        {
            KpiId = kpiId,
            Title = EditTitle,
            Description = EditDescription,
            TargetValue = targetValue,
            Unit = EditUnit,
            WeightPercent = weightPercent,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        };

        return true;
    }

    private void ClearCreateForm()
    {
        NewTitle = string.Empty;
        NewDescription = string.Empty;
        NewTargetValueText = string.Empty;
        NewActualValueText = "0";
        NewUnit = string.Empty;
        NewWeightPercentText = "100";
        NewPeriodStartText = DateTime.Today.ToString("dd.MM.yyyy");
        NewPeriodEndText = DateTime.Today.AddMonths(1).ToString("dd.MM.yyyy");
        SelectedEmployeeOption = EmployeeOptions.FirstOrDefault();
        CreateFormMessage = string.Empty;
    }

    private void ClearMessages()
    {
        StatusMessage = string.Empty;
        CreateFormMessage = string.Empty;
        EditFormMessage = string.Empty;
        ProgressFormMessage = string.Empty;
    }

    private void UpdateSummary()
    {
        TotalKpiCount = Kpis.Count;
        CompletedKpiCount = Kpis.Count(kpi => kpi.Kpi.Status == KpiStatus.Completed);
        OverdueKpiCount = Kpis.Count(kpi => kpi.Kpi.Status == KpiStatus.Overdue);

        AverageCompletionText = Kpis.Count == 0
            ? "0%"
            : $"{Math.Round(Kpis.Average(kpi => kpi.Kpi.CompletionPercent), 2)}%";
    }

    private static bool TryParseDouble(string value, out double number)
    {
        value = value.Trim().Replace(',', '.');

        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out number);
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        return DateTime.TryParseExact(
                value.Trim(),
                ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"],
                CultureInfo.GetCultureInfo("ru-RU"),
                DateTimeStyles.None,
                out date)
            || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    partial void OnSelectedKpiChanged(KpiRowViewModel? value)
    {
        if (IsCreateFormVisible || IsEditFormVisible || IsProgressFormVisible)
        {
            HideFormsAfterSelectionChanged();
        }

        OnPropertyChanged(nameof(HasSelectedKpi));
        OnPropertyChanged(nameof(HasNoSelectedKpi));
        OnPropertyChanged(nameof(CanShowKpiActions));
        OnPropertyChanged(nameof(CanShowActionPrompt));
        OnPropertyChanged(nameof(CanShowKpiDetails));
        OnPropertyChanged(nameof(SelectedKpiCaption));
    }

    partial void OnIsCreateFormVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanShowKpiDetails));
    }

    partial void OnIsEditFormVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanShowKpiDetails));
    }

    partial void OnIsProgressFormVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanShowKpiDetails));
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    partial void OnCreateFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasCreateFormMessage));
    }

    partial void OnEditFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasEditFormMessage));
    }

    partial void OnProgressFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasProgressFormMessage));
    }
}

public sealed class KpiRowViewModel
{
    public KpiItem Kpi { get; }

    private readonly EmployeeProfile? _employee;

    public string EmployeeTitle => _employee is null
        ? "Сотрудник не найден"
        : $"{_employee.FullName} · {_employee.PersonnelNumber}";

    public string Title => Kpi.Title;

    public string Description => string.IsNullOrWhiteSpace(Kpi.Description)
        ? "Без описания"
        : Kpi.Description;

    public string TargetValueText => $"{Kpi.TargetValue:0.##} {Kpi.Unit}";

    public string ActualValueText => $"{Kpi.ActualValue:0.##} {Kpi.Unit}";

    public string CompletionText => $"{Kpi.CompletionPercent:0.##}%";

    public string WeightText => $"{Kpi.WeightPercent:0.##}%";

    public string PeriodText => $"{Kpi.PeriodStart:dd.MM.yyyy} — {Kpi.PeriodEnd:dd.MM.yyyy}";

    public string StatusTitle => DisplayNames.ForKpiStatus(Kpi.Status);

    public string CreatedAtText => Kpi.CreatedAt.ToString("dd.MM.yyyy HH:mm");

    public string UpdatedAtText => Kpi.UpdatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    public string CompletedAtText => Kpi.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    public string OverfulfilledText => Kpi.IsOverfulfilled ? "Да" : "Нет";

    public KpiRowViewModel(KpiItem kpi, EmployeeProfile? employee)
    {
        Kpi = kpi;
        _employee = employee;
    }
}

public sealed class KpiEmployeeOption
{
    public EmployeeProfile Employee { get; }

    public string Title => $"{Employee.FullName} ({Employee.PersonnelNumber})";

    public KpiEmployeeOption(EmployeeProfile employee)
    {
        Employee = employee;
    }
}