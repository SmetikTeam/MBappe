using MBappe.Models;
using System;
using System.Linq;

namespace MBappe.ViewModels.Profile;

public sealed class ProfileViewModel : ViewModelBase
{
    public AppUser User { get; }

    public string RoleTitle => DisplayNames.ForRole(User.Role);

    public string StatusTitle => User.IsActive ? "Активен" : "Заблокирован";

    public string CreatedAtText => User.CreatedAt.ToString("dd.MM.yyyy");

    public string LastLoginText => User.LastLoginAt?.ToString("dd.MM.yyyy HH:mm") ?? "Нет данных";

    public string Initials => BuildInitials(User.FullName);

    public ProfileViewModel(AppUser user)
    {
        User = user;
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
