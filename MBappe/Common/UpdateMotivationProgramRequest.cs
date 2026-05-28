using System;

namespace MBappe.Common;

public class UpdateMotivationProgramRequest
{
    public Guid ProgramId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal BaseAmount { get; set; }

    public double MinEfficiencyPercent { get; set; } = 60;

    public double MaxEfficiencyPercent { get; set; } = 120;

    public bool IsActive { get; set; } = true;
}