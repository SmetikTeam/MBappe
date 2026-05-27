using MBappe.Models;
using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Profile;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly EmployeeService _employeeService;

    public AppUser User { get; }

    public string RoleTitle => DisplayNames.ForRole(User.Role);

    public string AccountStatusTitle => User.IsActive ? "Активна" : "Заблокирована";

    public string CreatedAtText => User.CreatedAt.ToString("dd.MM.yyyy");

    public string LastLoginText => User.LastLoginAt?.ToString("dd.MM.yyyy HH:mm") ?? "Нет данных";

    public string DisplayFullName => HasEmployeeProfile ? EmployeeFullName : User.FullName;

    public string Initials => BuildInitials(DisplayFullName);

    public bool HasNoEmployeeProfile => !HasEmployeeProfile && !string.IsNullOrWhiteSpace(ProfileMessage);

    [ObservableProperty]
    private bool hasEmployeeProfile;

    [ObservableProperty]
    private string profileMessage = string.Empty;

    [ObservableProperty]
    private string employeeFullName = string.Empty;

    [ObservableProperty]
    private string position = string.Empty;

    [ObservableProperty]
    private string department = string.Empty;

    [ObservableProperty]
    private string personnelNumber = string.Empty;

    [ObservableProperty]
    private string employeeStatusTitle = string.Empty;

    [ObservableProperty]
    private string employeeEmail = string.Empty;

    [ObservableProperty]
    private string employeePhone = string.Empty;

    public ProfileViewModel(AppUser user, EmployeeService employeeService)
    {
        User = user;
        _employeeService = employeeService;
        _ = LoadEmployeeProfileAsync();
    }

    private async Task LoadEmployeeProfileAsync()
    {
        var result = await _employeeService.GetCurrentEmployeeProfileAsync();

        if (!result.Success || result.Employee is null)
        {
            HasEmployeeProfile = false;
            ProfileMessage = result.Message;
            return;
        }

        var employee = result.Employee;
        EmployeeFullName = employee.FullName;
        Position = employee.Position;
        Department = employee.Department;
        PersonnelNumber = employee.PersonnelNumber;
        EmployeeStatusTitle = DisplayNames.ForEmployeeStatus(employee.Status);
        EmployeeEmail = employee.Email;
        EmployeePhone = string.IsNullOrWhiteSpace(employee.Phone) ? "-" : employee.Phone;
        ProfileMessage = string.Empty;
        HasEmployeeProfile = true;
    }

    private static string BuildInitials(string fullName)
    {
        var initials = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]))
            .Take(2)
            .ToArray();

        return initials.Length == 0 ? "MB" : new string(initials);
    }

    partial void OnHasEmployeeProfileChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoEmployeeProfile));
        OnPropertyChanged(nameof(DisplayFullName));
        OnPropertyChanged(nameof(Initials));
    }

    partial void OnProfileMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasNoEmployeeProfile));
    }

    partial void OnEmployeeFullNameChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayFullName));
        OnPropertyChanged(nameof(Initials));
    }
}
