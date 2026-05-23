using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
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
        var result = await AppServices.AuthService.LoginAsync("admin", "12345");

        Debug.WriteLine(result.Message);

        if (result.Success && result.User is not null)
        {
            Debug.WriteLine($"Пользователь: {result.User.FullName}");
            Debug.WriteLine($"Роль: {result.User.Role}");
        }
    }
}