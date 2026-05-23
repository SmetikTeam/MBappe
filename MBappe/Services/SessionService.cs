using MBappe.Models;
using System.Linq;

namespace MBappe.Services;

public class SessionService
{
    public AppUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public void StartSession(AppUser user)
    {
        CurrentUser = user;
    }

    public void EndSession()
    {
        CurrentUser = null;
    }

    public bool HasRole(UserRole role)
    {
        return CurrentUser?.Role == role;
    }

    public bool HasAnyRole(params UserRole[] roles)
    {
        if (CurrentUser is null)
            return false;

        return roles.Contains(CurrentUser.Role);
    }
}