using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using MBappe.Services;
using MBappe.ViewModels;
using MBappe.Views;

namespace MBappe;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();

        await MotivationDebugScenario.RunAsync(
            AppServices.AuthService,
            AppServices.UserManagementService,
            AppServices.EmployeeService,
            AppServices.KpiService,
            AppServices.MotivationService,
            AppServices.AuditLogService);
    }
}
