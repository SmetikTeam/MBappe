using System;

namespace MBappe.Models;

public class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Guid? UserId { get; set; }

    public string? UserLogin { get; set; }

    public UserRole? UserRole { get; set; }

    public AuditActionType ActionType { get; set; }

    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }
}