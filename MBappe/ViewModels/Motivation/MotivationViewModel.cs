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

namespace MBappe.ViewModels.Motivation;

public partial class MotivationViewModel : ViewModelBase
{
    private readonly MotivationService _motivationService;
    private readonly EmployeeService _employeeService;

    private IReadOnlyDictionary<Guid, EmployeeProfile> _employeesById = new Dictionary<Guid, EmployeeProfile>();
    private IReadOnlyDictionary<Guid, MotivationProgram> _programsById = new Dictionary<Guid, MotivationProgram>();

    [ObservableProperty]
    private ObservableCollection<MotivationBonusRowViewModel> bonuses = [];

    [ObservableProperty]
    private ObservableCollection<MotivationProgramOption> programOptions = [];

    [ObservableProperty]
    private ObservableCollection<MotivationEmployeeOption> employeeOptions = [];

    [ObservableProperty]
    private MotivationBonusRowViewModel? selectedBonus;

    [ObservableProperty]
    private MotivationProgramOption? selectedProgramOption;

    [ObservableProperty]
    private MotivationEmployeeOption? selectedEmployeeOption;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string calculateFormMessage = string.Empty;

    [ObservableProperty]
    private string programFormMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isCalculateFormVisible;

    [ObservableProperty]
    private bool isProgramFormVisible;

    [ObservableProperty]
    private string periodStartText = DateTime.Today.AddMonths(-1).ToString("dd.MM.yyyy");

    [ObservableProperty]
    private string periodEndText = DateTime.Today.ToString("dd.MM.yyyy");

    [ObservableProperty]
    private string newProgramTitle = string.Empty;

    [ObservableProperty]
    private string newProgramDescription = string.Empty;

    [ObservableProperty]
    private string newProgramBaseAmountText = "10000";

    [ObservableProperty]
    private string newProgramMinEfficiencyText = "60";

    [ObservableProperty]
    private string newProgramMaxEfficiencyText = "120";

    [ObservableProperty]
    private string rejectComment = string.Empty;

    [ObservableProperty]
    private int totalBonusCount;

    [ObservableProperty]
    private int pendingBonusCount;

    [ObservableProperty]
    private int approvedBonusCount;

    [ObservableProperty]
    private int paidBonusCount;

    [ObservableProperty]
    private string totalPayableAmountText = "0";

    public bool CanManageMotivation => _motivationService.CanManageMotivation();

    public bool CanCalculateBonuses => _motivationService.CanCalculateBonuses();

    public bool IsReadOnlyAccess => !CanCalculateBonuses && !CanManageMotivation;

    public bool HasSelectedBonus => SelectedBonus is not null;

    public bool HasNoSelectedBonus => SelectedBonus is null;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasCalculateFormMessage => !string.IsNullOrWhiteSpace(CalculateFormMessage);

    public bool HasProgramFormMessage => !string.IsNullOrWhiteSpace(ProgramFormMessage);

    public bool CanShowBonusDetails =>
        HasSelectedBonus
        && !IsCalculateFormVisible
        && !IsProgramFormVisible;

    public bool CanShowActionPrompt =>
        HasNoSelectedBonus
        && !IsCalculateFormVisible
        && !IsProgramFormVisible;

    public bool CanShowBonusActions =>
        HasSelectedBonus
        && CanManageMotivation
        && !IsCalculateFormVisible
        && !IsProgramFormVisible;

    public bool CanApproveSelectedBonus =>
        SelectedBonus?.Bonus.Status == MotivationBonusStatus.PendingApproval
        && CanManageMotivation;

    public bool CanRejectSelectedBonus =>
        SelectedBonus?.Bonus.Status == MotivationBonusStatus.PendingApproval
        && CanManageMotivation;

    public bool CanPaySelectedBonus =>
        SelectedBonus?.Bonus.Status == MotivationBonusStatus.Approved
        && CanManageMotivation;

    public bool CanCancelSelectedBonus =>
        SelectedBonus is not null
        && SelectedBonus.Bonus.Status is not MotivationBonusStatus.Paid
        && SelectedBonus.Bonus.Status is not MotivationBonusStatus.Cancelled
        && CanManageMotivation;

    public string SelectedBonusCaption => SelectedBonus is null
        ? "Бонус не выбран"
        : $"{SelectedBonus.EmployeeTitle} · {SelectedBonus.AmountText}";

    public MotivationViewModel(
        MotivationService motivationService,
        EmployeeService employeeService)
    {
        _motivationService = motivationService;
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

        var selectedBonusId = SelectedBonus?.Bonus.Id;

        await LoadEmployeesAsync();
        await LoadProgramsAsync();

        var result = await _motivationService.GetVisibleBonusesAsync();

        IsBusy = false;

        if (!result.Success || result.Bonuses is null)
        {
            StatusMessage = result.Message;
            return;
        }

        Bonuses = new ObservableCollection<MotivationBonusRowViewModel>(
            result.Bonuses.Select(bonus =>
            {
                _employeesById.TryGetValue(bonus.EmployeeId, out var employee);
                _programsById.TryGetValue(bonus.ProgramId, out var program);

                return new MotivationBonusRowViewModel(bonus, employee, program);
            }));

        SelectedBonus = selectedBonusId is null
            ? Bonuses.FirstOrDefault()
            : Bonuses.FirstOrDefault(bonus => bonus.Bonus.Id == selectedBonusId) ?? Bonuses.FirstOrDefault();

        UpdateSummary();

        StatusMessage = result.Message;
    }

    [RelayCommand]
    private void OpenCalculateForm()
    {
        if (!CanCalculateBonuses)
            return;

        ClearMessages();

        IsCalculateFormVisible = true;
        IsProgramFormVisible = false;

        SelectedEmployeeOption = EmployeeOptions.FirstOrDefault();
        SelectedProgramOption = ProgramOptions.FirstOrDefault(program => program.Program.IsActive)
            ?? ProgramOptions.FirstOrDefault();
        PeriodStartText = DateTime.Today.AddMonths(-1).ToString("dd.MM.yyyy");
        PeriodEndText = DateTime.Today.ToString("dd.MM.yyyy");
    }

    [RelayCommand]
    private void OpenProgramForm()
    {
        if (!CanManageMotivation)
            return;

        ClearMessages();

        IsCalculateFormVisible = false;
        IsProgramFormVisible = true;

        NewProgramTitle = string.Empty;
        NewProgramDescription = string.Empty;
        NewProgramBaseAmountText = "10000";
        NewProgramMinEfficiencyText = "60";
        NewProgramMaxEfficiencyText = "120";
    }

    [RelayCommand]
    private void CloseForms()
    {
        IsCalculateFormVisible = false;
        IsProgramFormVisible = false;
        ClearMessages();
    }

    [RelayCommand]
    private async Task CalculateBonusAsync()
    {
        if (!CanCalculateBonuses)
            return;

        if (SelectedEmployeeOption is null)
        {
            CalculateFormMessage = "Выберите сотрудника";
            return;
        }

        if (SelectedProgramOption is null)
        {
            CalculateFormMessage = "Выберите программу мотивации";
            return;
        }

        if (!TryParseDate(PeriodStartText, out var periodStart))
        {
            CalculateFormMessage = "Введите дату начала периода в формате дд.мм.гггг";
            return;
        }

        if (!TryParseDate(PeriodEndText, out var periodEnd))
        {
            CalculateFormMessage = "Введите дату окончания периода в формате дд.мм.гггг";
            return;
        }

        var result = await _motivationService.CalculateBonusAsync(new CalculateBonusRequest
        {
            EmployeeId = SelectedEmployeeOption.Employee.Id,
            ProgramId = SelectedProgramOption.Program.Id,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });

        if (!result.Success)
        {
            CalculateFormMessage = result.Message;
            return;
        }

        await RefreshAsync();

        IsCalculateFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task CreateProgramAsync()
    {
        if (!CanManageMotivation)
            return;

        if (!TryParseDecimal(NewProgramBaseAmountText, out var baseAmount))
        {
            ProgramFormMessage = "Введите базовую сумму числом";
            return;
        }

        if (!TryParseDouble(NewProgramMinEfficiencyText, out var minEfficiency))
        {
            ProgramFormMessage = "Введите минимальную эффективность числом";
            return;
        }

        if (!TryParseDouble(NewProgramMaxEfficiencyText, out var maxEfficiency))
        {
            ProgramFormMessage = "Введите максимальную эффективность числом";
            return;
        }

        var result = await _motivationService.CreateProgramAsync(new CreateMotivationProgramRequest
        {
            Title = NewProgramTitle,
            Description = NewProgramDescription,
            BaseAmount = baseAmount,
            MinEfficiencyPercent = minEfficiency,
            MaxEfficiencyPercent = maxEfficiency
        });

        if (!result.Success)
        {
            ProgramFormMessage = result.Message;
            return;
        }

        await RefreshAsync();

        IsProgramFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task ApproveSelectedBonusAsync()
    {
        if (SelectedBonus is null)
        {
            StatusMessage = "Выберите бонус";
            return;
        }

        var selectedBonusId = SelectedBonus.Bonus.Id;

        var result = await _motivationService.ApproveBonusAsync(selectedBonusId);
        StatusMessage = result.Message;

        if (!result.Success)
            return;

        await RefreshAsync();
        SelectedBonus = Bonuses.FirstOrDefault(bonus => bonus.Bonus.Id == selectedBonusId);
    }

    [RelayCommand]
    private async Task RejectSelectedBonusAsync()
    {
        if (SelectedBonus is null)
        {
            StatusMessage = "Выберите бонус";
            return;
        }

        var selectedBonusId = SelectedBonus.Bonus.Id;

        var result = await _motivationService.RejectBonusAsync(
            selectedBonusId,
            RejectComment);

        StatusMessage = result.Message;

        if (!result.Success)
            return;

        RejectComment = string.Empty;

        await RefreshAsync();
        SelectedBonus = Bonuses.FirstOrDefault(bonus => bonus.Bonus.Id == selectedBonusId);
    }

    [RelayCommand]
    private async Task PaySelectedBonusAsync()
    {
        if (SelectedBonus is null)
        {
            StatusMessage = "Выберите бонус";
            return;
        }

        var selectedBonusId = SelectedBonus.Bonus.Id;

        var result = await _motivationService.MarkBonusAsPaidAsync(selectedBonusId);
        StatusMessage = result.Message;

        if (!result.Success)
            return;

        await RefreshAsync();
        SelectedBonus = Bonuses.FirstOrDefault(bonus => bonus.Bonus.Id == selectedBonusId);
    }

    [RelayCommand]
    private async Task CancelSelectedBonusAsync()
    {
        if (SelectedBonus is null)
        {
            StatusMessage = "Выберите бонус";
            return;
        }

        var selectedBonusId = SelectedBonus.Bonus.Id;

        var result = await _motivationService.CancelBonusAsync(selectedBonusId);
        StatusMessage = result.Message;

        if (!result.Success)
            return;

        await RefreshAsync();
        SelectedBonus = Bonuses.FirstOrDefault(bonus => bonus.Bonus.Id == selectedBonusId);
    }

    private async Task LoadEmployeesAsync()
    {
        EmployeeOptions = [];

        var employeesResult = CanCalculateBonuses
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

        EmployeeOptions = new ObservableCollection<MotivationEmployeeOption>(
            employees
                .Where(employee => employee.Status != EmployeeStatus.Dismissed)
                .OrderBy(employee => employee.FullName)
                .Select(employee => new MotivationEmployeeOption(employee)));

        SelectedEmployeeOption = EmployeeOptions.FirstOrDefault();
    }

    private async Task LoadProgramsAsync()
    {
        ProgramOptions = [];

        var result = await _motivationService.GetProgramsAsync();

        if (!result.Success || result.Programs is null)
        {
            _programsById = new Dictionary<Guid, MotivationProgram>();
            return;
        }

        _programsById = result.Programs.ToDictionary(program => program.Id);

        ProgramOptions = new ObservableCollection<MotivationProgramOption>(
            result.Programs
                .OrderByDescending(program => program.IsActive)
                .ThenBy(program => program.Title)
                .Select(program => new MotivationProgramOption(program)));

        SelectedProgramOption = ProgramOptions.FirstOrDefault(program => program.Program.IsActive)
            ?? ProgramOptions.FirstOrDefault();
    }

    private void HideFormsAfterSelectionChanged()
    {
        IsCalculateFormVisible = false;
        IsProgramFormVisible = false;

        CalculateFormMessage = string.Empty;
        ProgramFormMessage = string.Empty;
    }

    private void ClearMessages()
    {
        StatusMessage = string.Empty;
        CalculateFormMessage = string.Empty;
        ProgramFormMessage = string.Empty;
    }

    private void UpdateSummary()
    {
        TotalBonusCount = Bonuses.Count;
        PendingBonusCount = Bonuses.Count(bonus => bonus.Bonus.Status == MotivationBonusStatus.PendingApproval);
        ApprovedBonusCount = Bonuses.Count(bonus => bonus.Bonus.Status == MotivationBonusStatus.Approved);
        PaidBonusCount = Bonuses.Count(bonus => bonus.Bonus.Status == MotivationBonusStatus.Paid);

        var totalPayable = Bonuses
            .Where(bonus => bonus.Bonus.Status is MotivationBonusStatus.PendingApproval or MotivationBonusStatus.Approved)
            .Sum(bonus => bonus.Bonus.FinalAmount);

        TotalPayableAmountText = $"{totalPayable:0.##}";
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

    private static bool TryParseDecimal(string value, out decimal number)
    {
        value = value.Trim().Replace(',', '.');

        return decimal.TryParse(
            value,
            NumberStyles.Number,
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

    partial void OnSelectedBonusChanged(MotivationBonusRowViewModel? value)
    {
        if (IsCalculateFormVisible || IsProgramFormVisible)
        {
            HideFormsAfterSelectionChanged();
        }

        OnPropertyChanged(nameof(HasSelectedBonus));
        OnPropertyChanged(nameof(HasNoSelectedBonus));
        OnPropertyChanged(nameof(CanShowBonusDetails));
        OnPropertyChanged(nameof(CanShowActionPrompt));
        OnPropertyChanged(nameof(CanShowBonusActions));
        OnPropertyChanged(nameof(CanApproveSelectedBonus));
        OnPropertyChanged(nameof(CanRejectSelectedBonus));
        OnPropertyChanged(nameof(CanPaySelectedBonus));
        OnPropertyChanged(nameof(CanCancelSelectedBonus));
        OnPropertyChanged(nameof(SelectedBonusCaption));
    }

    partial void OnIsCalculateFormVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanShowBonusDetails));
        OnPropertyChanged(nameof(CanShowActionPrompt));
        OnPropertyChanged(nameof(CanShowBonusActions));
    }

    partial void OnIsProgramFormVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanShowBonusDetails));
        OnPropertyChanged(nameof(CanShowActionPrompt));
        OnPropertyChanged(nameof(CanShowBonusActions));
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    partial void OnCalculateFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasCalculateFormMessage));
    }

    partial void OnProgramFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasProgramFormMessage));
    }
}

public sealed class MotivationBonusRowViewModel
{
    public MotivationBonus Bonus { get; }

    private readonly EmployeeProfile? _employee;
    private readonly MotivationProgram? _program;

    public string EmployeeTitle => _employee is null
        ? "Сотрудник не найден"
        : $"{_employee.FullName} · {_employee.PersonnelNumber}";

    public string ProgramTitle => _program?.Title ?? "Программа не найдена";

    public string PeriodText => $"{Bonus.PeriodStart:dd.MM.yyyy} — {Bonus.PeriodEnd:dd.MM.yyyy}";

    public string EfficiencyText => $"{Bonus.EfficiencyPercent:0.##}%";

    public string BaseAmountText => $"{Bonus.BaseAmount:0.##}";

    public string CalculatedAmountText => $"{Bonus.CalculatedAmount:0.##}";

    public string AmountText => $"{Bonus.FinalAmount:0.##}";

    public string StatusTitle => DisplayNames.ForMotivationBonusStatus(Bonus.Status);

    public string CommentText => string.IsNullOrWhiteSpace(Bonus.Comment)
        ? "Комментарий отсутствует"
        : Bonus.Comment;

    public string CreatedAtText => Bonus.CreatedAt.ToString("dd.MM.yyyy HH:mm");

    public string ApprovedAtText => Bonus.ApprovedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    public string PaidAtText => Bonus.PaidAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    public MotivationBonusRowViewModel(
        MotivationBonus bonus,
        EmployeeProfile? employee,
        MotivationProgram? program)
    {
        Bonus = bonus;
        _employee = employee;
        _program = program;
    }
}

public sealed class MotivationProgramOption
{
    public MotivationProgram Program { get; }

    public string Title => Program.IsActive
        ? $"{Program.Title} · {Program.BaseAmount:0.##}"
        : $"{Program.Title} · неактивна";

    public MotivationProgramOption(MotivationProgram program)
    {
        Program = program;
    }
}

public sealed class MotivationEmployeeOption
{
    public EmployeeProfile Employee { get; }

    public string Title => $"{Employee.FullName} ({Employee.PersonnelNumber})";

    public MotivationEmployeeOption(EmployeeProfile employee)
    {
        Employee = employee;
    }
}