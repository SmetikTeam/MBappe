using MBappe.Models;

namespace MBappe.Common;

public class RegisterRequest
{
    public string Login { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Employee;
}