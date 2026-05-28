using System;

namespace MBappe.Models;

public class KpiItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double TargetValue { get; set; }

    public double ActualValue { get; set; }

    public string Unit { get; set; } = string.Empty;

    public double WeightPercent { get; set; } = 100;

    public DateTime PeriodStart { get; set; } = DateTime.Today;

    public DateTime PeriodEnd { get; set; } = DateTime.Today.AddMonths(1);

    public KpiStatus Status { get; set; } = KpiStatus.InProgress;

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public double CompletionPercent
    {
        get
        {
            if (TargetValue <= 0)
                return 0;

            return Math.Round(ActualValue / TargetValue * 100, 2);
        }
    }

    public double CappedCompletionPercent => Math.Min(CompletionPercent, 120);

    public bool IsOverfulfilled => CompletionPercent > 100;
}