using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using MBappe.Common;
using MBappe.Models;
using MBappe.Services;
using MBappe.ViewModels;
using MBappe.Views;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        await TestAuthModule();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task TestAuthModule()
    {
        var failedResult = await AppServices.AuthService.LoginAsync("admin", "wrong");
        Debug.WriteLine(failedResult.Message);

        var successResult = await AppServices.AuthService.LoginAsync("admin", "12345");
        Debug.WriteLine(successResult.Message);

        var registerResult = await AppServices.AuthService.RegisterAsync(new RegisterRequest
        {
            Login = "testuser",
            Email = "testuser@mbappe.local",
            FullName = "Тестовый Пользователь",
            Password = "12345",
            ConfirmPassword = "12345",
            Role = UserRole.Employee
        });

        Debug.WriteLine(registerResult.Message);

        await AppServices.AuthService.LogoutAsync();

        var logs = await AppServices.AuditLogService.GetAllAsync();

        Debug.WriteLine("=== Журнал действий ===");

        foreach (var log in logs)
        {
            Debug.WriteLine(
                $"{log.CreatedAt:dd.MM.yyyy HH:mm:ss} | " +
                $"{log.ActionType} | " +
                $"Success: {log.IsSuccess} | " +
                $"Login: {log.UserLogin ?? "-"} | " +
                $"{log.Message} | " +
                $"{log.Details}");
        }
    }
}