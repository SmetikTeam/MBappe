using System;

namespace MBappe.Common;

public class CreateEmployeeRequest
{
    public Guid UserId { get; set; }

    public string PersonnelNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public Guid? ManagerEmployeeId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public DateTime HireDate { get; set; } = DateTime.Today;
}
