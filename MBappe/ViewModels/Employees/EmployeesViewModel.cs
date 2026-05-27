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

namespace MBappe.ViewModels.Employees;

public partial class EmployeesViewModel : ViewModelBase
{
    private readonly EmployeeService _employeeService;
    private readonly UserManagementService _userManagementService;
    private IReadOnlyDictionary<Guid, AppUser> _usersById = new Dictionary<Guid, AppUser>();

    [ObservableProperty]
    private ObservableCollection<EmployeeRowViewModel> employees = [];

    [ObservableProperty]
    private ObservableCollection<UserAccountOption> availableUserOptions = [];

    [ObservableProperty]
    private ObservableCollection<EmployeeManagerOption> managerOptions = [];

    [ObservableProperty]
    private EmployeeRowViewModel? selectedEmployee;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string createFormMessage = string.Empty;

    [ObservableProperty]
    private string editFormMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isCreateFormVisible;

    [ObservableProperty]
    private bool isEditFormVisible;

    [ObservableProperty]
    private UserAccountOption? selectedUserOption;

    [ObservableProperty]
    private EmployeeManagerOption? selectedNewManager;

    [ObservableProperty]
    private EmployeeManagerOption? selectedEditManager;

    [ObservableProperty]
    private EmployeeStatusOption selectedStatus = EmployeeStatusOption.All[0];

    [ObservableProperty]
    private string newPersonnelNumber = string.Empty;

    [ObservableProperty]
    private string newFullName = string.Empty;

    [ObservableProperty]
    private string newPosition = string.Empty;

    [ObservableProperty]
    private string newDepartment = string.Empty;

    [ObservableProperty]
    private string newEmail = string.Empty;

    [ObservableProperty]
    private string newPhone = string.Empty;

    [ObservableProperty]
    private string newHireDateText = DateTime.Today.ToString("dd.MM.yyyy");

    [ObservableProperty]
    private string editFullName = string.Empty;

    [ObservableProperty]
    private string editPosition = string.Empty;

    [ObservableProperty]
    private string editDepartment = string.Empty;

    [ObservableProperty]
    private string editEmail = string.Empty;

    [ObservableProperty]
    private string editPhone = string.Empty;

    public bool CanManageEmployees => _employeeService.CanManageEmployees();

    public bool IsReadOnlyEmployeeAccess => !CanManageEmployees;

    public bool HasSelectedEmployee => SelectedEmployee is not null;

    public bool HasNoSelectedEmployee => SelectedEmployee is null;

    public bool CanShowEmployeeActions => CanManageEmployees && HasSelectedEmployee;

    public bool CanShowActionPrompt => CanManageEmployees && HasNoSelectedEmployee;

    public bool CanShowEmployeeDetails => HasSelectedEmployee && !IsCreateFormVisible && !IsEditFormVisible;

    public IReadOnlyList<EmployeeStatusOption> StatusOptions { get; } = EmployeeStatusOption.All;

    public string SelectedEmployeeCaption => SelectedEmployee is null
        ? "Сотрудник не выбран"
        : $"{SelectedEmployee.FullName} · {SelectedEmployee.PersonnelNumber}";

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasCreateFormMessage => !string.IsNullOrWhiteSpace(CreateFormMessage);

    public bool HasEditFormMessage => !string.IsNullOrWhiteSpace(EditFormMessage);

    public EmployeesViewModel(
        EmployeeService employeeService,
        UserManagementService userManagementService)
    {
        _employeeService = employeeService;
        _userManagementService = userManagementService;
        ManagerOptions = new ObservableCollection<EmployeeManagerOption>([EmployeeManagerOption.Empty]);
        SelectedNewManager = ManagerOptions[0];
        SelectedEditManager = ManagerOptions[0];
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        var selectedEmployeeId = SelectedEmployee?.Employee.Id;
        var result = await _employeeService.GetAllEmployeesAsync();
        IsBusy = false;

        if (!result.Success || result.Employees is null)
        {
            StatusMessage = result.Message;
            return;
        }

        Employees = new ObservableCollection<EmployeeRowViewModel>(
            result.Employees.Select(employee => new EmployeeRowViewModel(
                employee,
                result.Employees.FirstOrDefault(manager => manager.Id == employee.ManagerEmployeeId))));

        SelectedEmployee = selectedEmployeeId is null
            ? null
            : Employees.FirstOrDefault(employee => employee.Employee.Id == selectedEmployeeId.Value);

        if (CanManageEmployees)
            await LoadAvailableUsersAsync();

        UpdateManagerOptions();
    }

    [RelayCommand]
    private async Task OpenCreateFormAsync()
    {
        if (!CanManageEmployees)
            return;

        ClearCreateForm();
        SelectedEmployee = null;
        IsCreateFormVisible = true;
        IsEditFormVisible = false;
        StatusMessage = string.Empty;
        EditFormMessage = string.Empty;
        await LoadAvailableUsersAsync();
        UpdateManagerOptions();

        if (AvailableUserOptions.Count == 0)
            CreateFormMessage = "Нет учетных записей без профиля сотрудника";
    }

    [RelayCommand]
    private void OpenEditForm()
    {
        if (!CanManageEmployees)
            return;

        if (SelectedEmployee is null)
        {
            StatusMessage = "Выберите сотрудника";
            return;
        }

        var employee = SelectedEmployee.Employee;
        EditFullName = employee.FullName;
        EditPosition = employee.Position;
        EditDepartment = employee.Department;
        EditEmail = employee.Email;
        EditPhone = employee.Phone;
        IsEditFormVisible = true;
        IsCreateFormVisible = false;
        UpdateManagerOptions();
        SelectedEditManager = ManagerOptions.FirstOrDefault(option => option.EmployeeId == employee.ManagerEmployeeId)
            ?? ManagerOptions[0];
        StatusMessage = string.Empty;
        CreateFormMessage = string.Empty;
        EditFormMessage = string.Empty;
    }

    [RelayCommand]
    private void CloseEmployeeForm()
    {
        IsCreateFormVisible = false;
        IsEditFormVisible = false;
        CreateFormMessage = string.Empty;
        EditFormMessage = string.Empty;
    }

    [RelayCommand]
    private async Task CreateEmployeeAsync()
    {
        if (!CanManageEmployees)
            return;

        if (SelectedUserOption is null)
        {
            CreateFormMessage = "Выберите учетную запись пользователя";
            return;
        }

        if (!TryParseDate(NewHireDateText, out var hireDate))
        {
            CreateFormMessage = "Введите дату приема в формате дд.мм.гггг";
            return;
        }

        CreateFormMessage = string.Empty;

        var request = new CreateEmployeeRequest
        {
            UserId = SelectedUserOption.User.Id,
            PersonnelNumber = NewPersonnelNumber,
            FullName = NewFullName,
            Position = NewPosition,
            Department = NewDepartment,
            ManagerEmployeeId = SelectedNewManager?.EmployeeId,
            Email = NewEmail,
            Phone = NewPhone,
            HireDate = hireDate
        };

        var result = await _employeeService.CreateEmployeeAsync(request);

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
    private async Task SaveEmployeeAsync()
    {
        if (!CanManageEmployees)
            return;

        if (SelectedEmployee is null)
        {
            EditFormMessage = "Выберите сотрудника";
            return;
        }

        EditFormMessage = string.Empty;

        var selectedEmployeeId = SelectedEmployee.Employee.Id;
        var request = new UpdateEmployeeRequest
        {
            EmployeeId = selectedEmployeeId,
            FullName = EditFullName,
            Position = EditPosition,
            Department = EditDepartment,
            ManagerEmployeeId = SelectedEditManager?.EmployeeId,
            Email = EditEmail,
            Phone = EditPhone
        };

        var result = await _employeeService.UpdateEmployeeAsync(request);

        if (!result.Success)
        {
            EditFormMessage = result.Message;
            return;
        }

        await RefreshAsync();
        SelectedEmployee = Employees.FirstOrDefault(employee => employee.Employee.Id == selectedEmployeeId);
        IsEditFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task ApplyStatusAsync()
    {
        if (!CanManageEmployees)
            return;

        if (SelectedEmployee is null)
        {
            StatusMessage = "Выберите сотрудника";
            return;
        }

        var selectedEmployeeId = SelectedEmployee.Employee.Id;
        var result = await _employeeService.ChangeEmployeeStatusAsync(selectedEmployeeId, SelectedStatus.Status);
        StatusMessage = result.Message;

        if (!result.Success)
            return;

        await RefreshAsync();
        SelectedEmployee = Employees.FirstOrDefault(employee => employee.Employee.Id == selectedEmployeeId);
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task DismissSelectedAsync()
    {
        if (SelectedEmployee is null)
            return;

        SelectedStatus = StatusOptions.First(option => option.Status == EmployeeStatus.Dismissed);
        await ApplyStatusAsync();
    }

    [RelayCommand]
    private async Task RestoreSelectedAsync()
    {
        if (SelectedEmployee is null)
            return;

        SelectedStatus = StatusOptions.First(option => option.Status == EmployeeStatus.Active);
        await ApplyStatusAsync();
    }

    private async Task LoadAvailableUsersAsync()
    {
        var usersResult = await _userManagementService.GetAllUsersAsync();

        if (!usersResult.Success || usersResult.Users is null)
        {
            _usersById = new Dictionary<Guid, AppUser>();
            AvailableUserOptions = [];
            return;
        }

        _usersById = usersResult.Users.ToDictionary(user => user.Id);

        var employeeUserIds = Employees
            .Select(employee => employee.Employee.UserId)
            .ToHashSet();

        AvailableUserOptions = new ObservableCollection<UserAccountOption>(
            usersResult.Users
                .Where(user => !employeeUserIds.Contains(user.Id))
                .OrderBy(user => user.FullName)
                .Select(user => new UserAccountOption(user)));

        SelectedUserOption = AvailableUserOptions.FirstOrDefault();
    }

    private void UpdateManagerOptions()
    {
        ManagerOptions = new ObservableCollection<EmployeeManagerOption>(
            new[] { EmployeeManagerOption.Empty }
                .Concat(Employees
                    .Where(employee => _usersById.TryGetValue(employee.Employee.UserId, out var user)
                        && user.Role is UserRole.HrSpecialist or UserRole.Manager
                        && (!IsEditFormVisible || employee.Employee.Id != SelectedEmployee?.Employee.Id))
                    .Select(employee => new EmployeeManagerOption(employee.Employee))));

        SelectedNewManager = ManagerOptions[0];
        SelectedEditManager = SelectedEmployee is null
            ? ManagerOptions[0]
            : ManagerOptions.FirstOrDefault(option => option.EmployeeId == SelectedEmployee.Employee.ManagerEmployeeId)
                ?? ManagerOptions[0];
    }

    private void ClearCreateForm()
    {
        NewPersonnelNumber = string.Empty;
        NewFullName = string.Empty;
        NewPosition = string.Empty;
        NewDepartment = string.Empty;
        NewEmail = string.Empty;
        NewPhone = string.Empty;
        NewHireDateText = DateTime.Today.ToString("dd.MM.yyyy");
        SelectedNewManager = ManagerOptions.Count > 0 ? ManagerOptions[0] : null;
        CreateFormMessage = string.Empty;
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

    partial void OnSelectedUserOptionChanged(UserAccountOption? value)
    {
        if (value is null)
            return;

        NewFullName = value.FullName;
        NewEmail = value.Email;
    }

    partial void OnSelectedEmployeeChanged(EmployeeRowViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedEmployee));
        OnPropertyChanged(nameof(HasNoSelectedEmployee));
        OnPropertyChanged(nameof(CanShowEmployeeActions));
        OnPropertyChanged(nameof(CanShowActionPrompt));
        OnPropertyChanged(nameof(CanShowEmployeeDetails));
        OnPropertyChanged(nameof(SelectedEmployeeCaption));
        UpdateManagerOptions();
        SelectedStatus = value is null
            ? StatusOptions[0]
            : StatusOptions.First(option => option.Status == value.Employee.Status);
    }

    partial void OnIsCreateFormVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanShowEmployeeDetails));
    }

    partial void OnIsEditFormVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanShowEmployeeDetails));
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
}

public sealed class EmployeeRowViewModel
{
    public EmployeeProfile Employee { get; }

    private readonly EmployeeProfile? _manager;

    public string PersonnelNumber => Employee.PersonnelNumber;

    public string FullName => Employee.FullName;

    public string Position => Employee.Position;

    public string Department => Employee.Department;

    public string Email => Employee.Email;

    public string Phone => string.IsNullOrWhiteSpace(Employee.Phone) ? "-" : Employee.Phone;

    public string HireDateText => Employee.HireDate.ToString("dd.MM.yyyy");

    public string DismissalDateText => Employee.DismissalDate?.ToString("dd.MM.yyyy") ?? "-";

    public string CreatedAtText => Employee.CreatedAt.ToString("dd.MM.yyyy HH:mm");

    public string UpdatedAtText => Employee.UpdatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    public string ManagerTitle => _manager is null
        ? "Без руководителя"
        : $"{_manager.FullName} · {_manager.PersonnelNumber}";

    public string StatusTitle => DisplayNames.ForEmployeeStatus(Employee.Status);

    public EmployeeRowViewModel(EmployeeProfile employee, EmployeeProfile? manager = null)
    {
        Employee = employee;
        _manager = manager;
    }
}

public sealed class EmployeeStatusOption
{
    public static IReadOnlyList<EmployeeStatusOption> All { get; } =
    [
        new EmployeeStatusOption(EmployeeStatus.Active),
        new EmployeeStatusOption(EmployeeStatus.OnVacation),
        new EmployeeStatusOption(EmployeeStatus.SickLeave),
        new EmployeeStatusOption(EmployeeStatus.Dismissed)
    ];

    public EmployeeStatus Status { get; }

    public string Title => DisplayNames.ForEmployeeStatus(Status);

    private EmployeeStatusOption(EmployeeStatus status)
    {
        Status = status;
    }
}

public sealed class UserAccountOption
{
    public AppUser User { get; }

    public string FullName => User.FullName;

    public string Email => User.Email;

    public string Title => $"{User.FullName} ({User.Login})";

    public UserAccountOption(AppUser user)
    {
        User = user;
    }
}

public sealed class EmployeeManagerOption
{
    public static EmployeeManagerOption Empty { get; } = new EmployeeManagerOption();

    public Guid? EmployeeId { get; }

    public string Title { get; }

    public EmployeeManagerOption(EmployeeProfile employee)
    {
        EmployeeId = employee.Id;
        Title = $"{employee.FullName} ({employee.PersonnelNumber})";
    }

    private EmployeeManagerOption()
    {
        EmployeeId = null;
        Title = "Без руководителя";
    }
}
