using System;

namespace MBappe.Models;

public class EmployeeProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string PersonnelNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public Guid? ManagerEmployeeId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public DateTime HireDate { get; set; } = DateTime.Today;

    public DateTime? DismissalDate { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
