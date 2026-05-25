using MBappe.Services;
using MBappe.ViewModels.Auth;
using MBappe.ViewModels.Shell;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MBappe.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase currentViewModel = null!;

    public MainWindowViewModel()
    {
        ShowLogin();
    }

    private void ShowLogin(string? message = null)
    {
        CurrentViewModel = new LoginViewModel(
            AppServices.AuthService,
            ShowShell,
            ShowRegister,
            message);
    }

    private void ShowRegister()
    {
        CurrentViewModel = new RegisterViewModel(
            AppServices.AuthService,
            ShowLogin);
    }

    private void ShowShell()
    {
        CurrentViewModel = new MainShellViewModel(
            AppServices.AuthService,
            AppServices.SessionService,
            AppServices.UserManagementService,
            AppServices.AuditLogService,
            ShowLogin);
    }
}
