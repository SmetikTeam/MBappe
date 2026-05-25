using MBappe.Common;
using MBappe.Models;
using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Auth;

public partial class RegisterViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly Action<string?> _openLogin;

    [ObservableProperty]
    private string login = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public RegisterViewModel(AuthService authService, Action<string?> openLogin)
    {
        _authService = authService;
        _openLogin = openLogin;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        var request = new RegisterRequest
        {
            Login = Login,
            Email = Email,
            FullName = FullName,
            Password = Password,
            ConfirmPassword = ConfirmPassword,
            Role = UserRole.Employee
        };

        var result = await _authService.RegisterAsync(request);

        IsBusy = false;

        if (!result.Success)
        {
            StatusMessage = result.Message;
            return;
        }

        _openLogin("Аккаунт создан. Войдите с новым логином.");
    }

    [RelayCommand]
    private void BackToLogin()
    {
        _openLogin(null);
    }
}
