using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Auth;

public partial class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly Action _openShell;
    private readonly Action _openRegister;

    [ObservableProperty]
    private string login = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isStatusSuccess;

    [ObservableProperty]
    private bool isBusy;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool IsStatusError => HasStatusMessage && !IsStatusSuccess;

    public LoginViewModel(
        AuthService authService,
        Action openShell,
        Action openRegister,
        string? initialMessage = null)
    {
        _authService = authService;
        _openShell = openShell;
        _openRegister = openRegister;
        IsStatusSuccess = !string.IsNullOrWhiteSpace(initialMessage);
        StatusMessage = initialMessage ?? string.Empty;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        IsStatusSuccess = false;
        StatusMessage = string.Empty;

        var result = await _authService.LoginAsync(Login, Password);

        IsBusy = false;

        if (!result.Success)
        {
            StatusMessage = result.Message;
            return;
        }

        _openShell();
    }

    [RelayCommand]
    private void OpenRegister()
    {
        _openRegister();
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
        OnPropertyChanged(nameof(IsStatusError));
    }

    partial void OnIsStatusSuccessChanged(bool value)
    {
        OnPropertyChanged(nameof(IsStatusError));
    }
}
