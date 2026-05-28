using MBappe.Models;
using MBappe.Services;
using MBappe.ViewModels.Audit;
using MBappe.ViewModels.Employees;
using MBappe.ViewModels.Profile;
using MBappe.ViewModels.Users;
using MBappe.ViewModels.Kpi;
using MBappe.ViewModels.Learning;
using MBappe.ViewModels.Motivation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Shell;

public partial class MainShellViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly UserManagementService _userManagementService;
    private readonly EmployeeService _employeeService;
    private readonly AuditLogService _auditLogService;
    private readonly Action<string?> _openLogin;
    private readonly KpiService _kpiService;
    private readonly LearningService _learningService;
    private readonly MotivationService _motivationService;

    [ObservableProperty]
    private NavigationItemViewModel? selectedNavigationItem;

    [ObservableProperty]
    private ViewModelBase currentPage = null!;

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];

    public AppUser CurrentUser { get; }

    public string CurrentUserName => CurrentUser.FullName;

    public string CurrentUserRole => DisplayNames.ForRole(CurrentUser.Role);

    public string CurrentUserLogin => CurrentUser.Login;

    public string UserInitials => BuildInitials(CurrentUser.FullName);

    public MainShellViewModel(
        AuthService authService,
        SessionService sessionService,
        UserManagementService userManagementService,
        EmployeeService employeeService,
        KpiService kpiService,
        LearningService learningService,
        MotivationService motivationService,
        AuditLogService auditLogService,
        Action<string?> openLogin)
    {
        _authService = authService;
        _userManagementService = userManagementService;
        _employeeService = employeeService;
        _kpiService = kpiService;
        _learningService = learningService;
        _motivationService = motivationService;
        _auditLogService = auditLogService;
        _openLogin = openLogin;

        CurrentUser = sessionService.CurrentUser
            ?? throw new InvalidOperationException("Main shell requires authenticated user.");

        BuildNavigation();
        SelectedNavigationItem = NavigationItems.FirstOrDefault();
    }

    partial void OnSelectedNavigationItemChanged(NavigationItemViewModel? value)
    {
        if (value is not null)
            CurrentPage = value.CreateViewModel();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        _openLogin("Вы вышли из системы.");
    }

    private void BuildNavigation()
    {
        if (CurrentUser.Role == UserRole.Employee)
        {
            NavigationItems.Add(new NavigationItemViewModel(
                "Профиль",
                "ПР",
                "Личный кадровый профиль",
                () => new ProfileViewModel(CurrentUser, _employeeService)));

            AddKpiNavigationItem();
            AddLearningNavigationItem();
            AddMotivationNavigationItem();


            return;
        }

        if (CurrentUser.Role == UserRole.Administrator)
            AddUsersNavigationItem();

        if (CurrentUser.Role is UserRole.Administrator or UserRole.HrSpecialist or UserRole.Manager)
            AddEmployeesNavigationItem();

        AddKpiNavigationItem();
        AddLearningNavigationItem();
        AddMotivationNavigationItem();

        if (CurrentUser.Role == UserRole.HrSpecialist)
            AddUsersNavigationItem();

        AddWorkInProgressModules();

        if (CurrentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            AddAuditNavigationItem();
    }

    private void AddMotivationNavigationItem()
    {
        NavigationItems.Add(new NavigationItemViewModel(
            "Мотивация",
            "₽",
            "Бонусы и премии",
            () => new MotivationViewModel(_motivationService, _employeeService)));
    }

    private void AddKpiNavigationItem()
    {
        NavigationItems.Add(new NavigationItemViewModel(
            "KPI",
            "KPI",
            "Показатели эффективности",
            () => new KpiViewModel(_kpiService, _employeeService)));
    }

    private void AddLearningNavigationItem()
    {
        NavigationItems.Add(new NavigationItemViewModel(
            "Обучение",
            "ОБ",
            "Программы развития",
            () => new LearningViewModel(_learningService, _employeeService)));
    }

    private void AddUsersNavigationItem()
    {
        NavigationItems.Add(new NavigationItemViewModel(
            "Пользователи",
            "ПЛ",
            "Учетные записи и роли",
            () => new UsersViewModel(_userManagementService)));
    }

    private void AddEmployeesNavigationItem()
    {
        NavigationItems.Add(new NavigationItemViewModel(
            "Сотрудники",
            "СТ",
            "Кадровые профили",
            () => new EmployeesViewModel(_employeeService, _userManagementService)));
    }

    private void AddWorkInProgressModules()
    {
        NavigationItems.Add(new NavigationItemViewModel(
            "Аналитика",
            "АН",
            "Отчеты по персоналу",
            () => ModulePlaceholderViewModel.CreateAnalyticsModule()));
    }

    private void AddAuditNavigationItem()
    {
        NavigationItems.Add(new NavigationItemViewModel(
            "Журнал",
            "ЖР",
            "Аудит безопасности",
            () => new AuditLogViewModel(_auditLogService)));
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
}
