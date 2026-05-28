using MBappe.Models;
using System;
using System.Collections.Generic;

namespace MBappe.Common;

public class EmployeeEfficiencyResult
{
    public Guid EmployeeId { get; }

    public DateTime PeriodStart { get; }

    public DateTime PeriodEnd { get; }

    public int KpiCount { get; }

    public double TotalWeight { get; }

    public double EfficiencyPercent { get; }

    public IReadOnlyList<KpiItem> Kpis { get; }

    public EmployeeEfficiencyResult(
        Guid employeeId,
        DateTime periodStart,
        DateTime periodEnd,
        int kpiCount,
        double totalWeight,
        double efficiencyPercent,
        IReadOnlyList<KpiItem> kpis)
    {
        EmployeeId = employeeId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        KpiCount = kpiCount;
        TotalWeight = totalWeight;
        EfficiencyPercent = efficiencyPercent;
        Kpis = kpis;
    }
}