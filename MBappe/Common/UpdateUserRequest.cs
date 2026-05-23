using System;

namespace MBappe.Common;

public class UpdateUserRequest
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}