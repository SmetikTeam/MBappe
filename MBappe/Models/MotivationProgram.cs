using System;

namespace MBappe.Models;

public class MotivationProgram
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal BaseAmount { get; set; }

    public double MinEfficiencyPercent { get; set; } = 60;

    public double MaxEfficiencyPercent { get; set; } = 120;

    public bool IsActive { get; set; } = true;

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}