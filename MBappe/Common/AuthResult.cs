using MBappe.Models;

namespace MBappe.Common;

public class AuthResult
{
    public bool Success { get; }

    public string Message { get; }

    public AppUser? User { get; }

    private AuthResult(bool success, string message, AppUser? user = null)
    {
        Success = success;
        Message = message;
        User = user;
    }

    public static AuthResult Ok(AppUser user, string message = "Успешная авторизация")
    {
        return new AuthResult(true, message, user);
    }

    public static AuthResult Fail(string message)
    {
        return new AuthResult(false, message);
    }
}