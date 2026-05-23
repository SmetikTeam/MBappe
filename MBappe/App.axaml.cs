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
        Debug.WriteLine("=== Тест управления пользователями ===");

        var adminLogin = await AppServices.AuthService.LoginAsync("admin", "12345");
        Debug.WriteLine(adminLogin.Message);

        var createResult = await AppServices.UserManagementService.CreateUserAsync(new CreateUserRequest
        {
            Login = "newuser",
            Email = "newuser@mbappe.local",
            FullName = "Новый Пользователь",
            Password = "12345",
            ConfirmPassword = "12345",
            Role = UserRole.Employee
        });

        Debug.WriteLine(createResult.Message);

        var usersResult = await AppServices.UserManagementService.GetAllUsersAsync();

        Debug.WriteLine(usersResult.Message);

        if (usersResult.Users is not null)
        {
            foreach (var user in usersResult.Users)
            {
                Debug.WriteLine(
                    $"{user.Login} | {user.FullName} | {user.Role} | Active: {user.IsActive}");
            }
        }

        if (createResult.User is not null)
        {
            var blockResult = await AppServices.UserManagementService.BlockUserAsync(createResult.User.Id);
            Debug.WriteLine(blockResult.Message);

            await AppServices.AuthService.LogoutAsync();

            var blockedLogin = await AppServices.AuthService.LoginAsync("newuser", "12345");
            Debug.WriteLine(blockedLogin.Message);

            var adminLoginAgain = await AppServices.AuthService.LoginAsync("admin", "12345");
            Debug.WriteLine(adminLoginAgain.Message);

            var unblockResult = await AppServices.UserManagementService.UnblockUserAsync(createResult.User.Id);
            Debug.WriteLine(unblockResult.Message);

            await AppServices.AuthService.LogoutAsync();

            var unblockedLogin = await AppServices.AuthService.LoginAsync("newuser", "12345");
            Debug.WriteLine(unblockedLogin.Message);
        }

        await AppServices.AuthService.LogoutAsync();

        var employeeLogin = await AppServices.AuthService.LoginAsync("employee", "12345");
        Debug.WriteLine(employeeLogin.Message);

        var deniedResult = await AppServices.UserManagementService.GetAllUsersAsync();
        Debug.WriteLine(deniedResult.Message);

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