using System;

namespace MBappe.Common;

public class CalculateBonusRequest
{
    public Guid EmployeeId { get; set; }

    public Guid ProgramId { get; set; }

    public DateTime PeriodStart { get; set; } = DateTime.Today.AddMonths(-1);

    public DateTime PeriodEnd { get; set; } = DateTime.Today;
}