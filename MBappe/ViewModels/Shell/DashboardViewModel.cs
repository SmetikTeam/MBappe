using MBappe.Models;
using System;
using System.Collections.ObjectModel;

namespace MBappe.ViewModels.Shell;

public sealed class DashboardViewModel : ViewModelBase
{
    public AppUser CurrentUser { get; }

    public string Title => "Рабочая панель";

    public string RoleTitle => DisplayNames.ForRole(CurrentUser.Role);

    public string UpdatedAt => DateTime.Now.ToString("dd.MM.yyyy HH:mm");

    public ObservableCollection<DashboardMetricViewModel> Metrics { get; }

    public DashboardViewModel(AppUser currentUser)
    {
        CurrentUser = currentUser;
        Metrics =
        [
            new DashboardMetricViewModel("Профиль", "Активен", "Учетная запись готова к работе"),
            new DashboardMetricViewModel("KPI", "Контур", "Раздел подготовлен для показателей эффективности"),
            new DashboardMetricViewModel("Обучение", "Контур", "Раздел подготовлен для программ развития"),
            new DashboardMetricViewModel("Мотивация", "Контур", "Раздел подготовлен для бонусов и премий")
        ];
    }
}

public sealed class DashboardMetricViewModel
{
    public string Title { get; }

    public string Value { get; }

    public string Caption { get; }

    public DashboardMetricViewModel(string title, string value, string caption)
    {
        Title = title;
        Value = value;
        Caption = caption;
    }
}
