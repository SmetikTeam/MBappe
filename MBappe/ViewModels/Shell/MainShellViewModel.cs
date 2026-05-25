using MBappe.Models;
using MBappe.Services;
using MBappe.ViewModels.Audit;
using MBappe.ViewModels.Profile;
using MBappe.ViewModels.Users;
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
    private readonly AuditLogService _auditLogService;
    private readonly Action<string?> _openLogin;

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
        AuditLogService auditLogService,
        Action<string?> openLogin)
    {
        _authService = authService;
        _userManagementService = userManagementService;
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
        NavigationItems.Add(new NavigationItemViewModel(
            "Обзор",
            "HR",
            "Состояние рабочего контура",
            () => new DashboardViewModel(CurrentUser)));

        NavigationItems.Add(new NavigationItemViewModel(
            "Профиль",
            "ПР",
            "Личный кабинет сотрудника",
            () => new ProfileViewModel(CurrentUser)));

        if (CanManageUsers(CurrentUser.Role))
        {
            NavigationItems.Add(new NavigationItemViewModel(
                "Сотрудники",
                "СТ",
                "Учетные записи и доступ",
                () => new UsersViewModel(_userManagementService)));
        }

        NavigationItems.Add(new NavigationItemViewModel(
            "KPI",
            "KPI",
            "Показатели эффективности",
            () => ModulePlaceholderViewModel.CreateKpiModule()));

        NavigationItems.Add(new NavigationItemViewModel(
            "Обучение",
            "ОБ",
            "Программы развития",
            () => ModulePlaceholderViewModel.CreateLearningModule()));

        NavigationItems.Add(new NavigationItemViewModel(
            "Мотивация",
            "МТ",
            "Бонусы и премии",
            () => ModulePlaceholderViewModel.CreateMotivationModule()));

        if (CurrentUser.Role is UserRole.Manager or UserRole.HrSpecialist or UserRole.Administrator)
        {
            NavigationItems.Add(new NavigationItemViewModel(
                "Аналитика",
                "АН",
                "Отчеты по персоналу",
                () => ModulePlaceholderViewModel.CreateAnalyticsModule()));
        }

        if (CurrentUser.Role == UserRole.Administrator)
        {
            NavigationItems.Add(new NavigationItemViewModel(
                "Журнал",
                "ЖР",
                "Аудит безопасности",
                () => new AuditLogViewModel(_auditLogService)));
        }
    }

    private static bool CanManageUsers(UserRole role)
    {
        return role is UserRole.Administrator or UserRole.HrSpecialist;
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
