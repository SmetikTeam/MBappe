using MBappe.Models;
using System.Collections.Generic;

namespace MBappe.Common;

public class UserOperationResult
{
    public bool Success { get; }

    public string Message { get; }

    public AppUser? User { get; }

    public IReadOnlyList<AppUser>? Users { get; }

    private UserOperationResult(
        bool success,
        string message,
        AppUser? user = null,
        IReadOnlyList<AppUser>? users = null)
    {
        Success = success;
        Message = message;
        User = user;
        Users = users;
    }

    public static UserOperationResult Ok(string message)
    {
        return new UserOperationResult(true, message);
    }

    public static UserOperationResult Ok(AppUser user, string message)
    {
        return new UserOperationResult(true, message, user);
    }

    public static UserOperationResult Ok(IReadOnlyList<AppUser> users, string message)
    {
        return new UserOperationResult(true, message, users: users);
    }

    public static UserOperationResult Fail(string message)
    {
        return new UserOperationResult(false, message);
    }
}