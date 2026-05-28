using System;

namespace MBappe.Common;

public class UpdateKpiRequest
{
    public Guid KpiId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double TargetValue { get; set; }

    public string Unit { get; set; } = string.Empty;

    public double WeightPercent { get; set; } = 100;

    public DateTime PeriodStart { get; set; } = DateTime.Today;

    public DateTime PeriodEnd { get; set; } = DateTime.Today.AddMonths(1);
}