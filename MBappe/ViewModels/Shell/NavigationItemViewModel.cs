using System;

namespace MBappe.ViewModels.Shell;

public sealed class NavigationItemViewModel : ViewModelBase
{
    private readonly Func<ViewModelBase> _createViewModel;

    public string Title { get; }

    public string IconKey { get; }

    public bool IsProfileIcon => IconKey == NavigationIconPack.Profile;

    public bool IsUsersIcon => IconKey == NavigationIconPack.Users;

    public bool IsEmployeesIcon => IconKey == NavigationIconPack.Employees;

    public bool IsKpiIcon => IconKey == NavigationIconPack.Kpi;

    public bool IsLearningIcon => IconKey == NavigationIconPack.Learning;

    public bool IsMotivationIcon => IconKey == NavigationIconPack.Motivation;

    public bool IsAnalyticsIcon => IconKey == NavigationIconPack.Analytics;

    public bool IsAuditIcon => IconKey == NavigationIconPack.Audit;

    public string Description { get; }

    public NavigationItemViewModel(
        string title,
        string iconKey,
        string description,
        Func<ViewModelBase> createViewModel)
    {
        Title = title;
        IconKey = iconKey;
        Description = description;
        _createViewModel = createViewModel;
    }

    public ViewModelBase CreateViewModel()
    {
        return _createViewModel();
    }
}

public static class NavigationIconPack
{
    public const string Profile = "Profile";
    public const string Users = "Users";
    public const string Employees = "Employees";
    public const string Kpi = "Kpi";
    public const string Learning = "Learning";
    public const string Motivation = "Motivation";
    public const string Analytics = "Analytics";
    public const string Audit = "Audit";
}
