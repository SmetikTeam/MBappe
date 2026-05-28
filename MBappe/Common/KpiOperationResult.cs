using MBappe.Models;
using System.Collections.Generic;

namespace MBappe.Common;

public class KpiOperationResult
{
    public bool Success { get; }

    public string Message { get; }

    public KpiItem? Kpi { get; }

    public IReadOnlyList<KpiItem>? Kpis { get; }

    public EmployeeEfficiencyResult? Efficiency { get; }

    private KpiOperationResult(
        bool success,
        string message,
        KpiItem? kpi = null,
        IReadOnlyList<KpiItem>? kpis = null,
        EmployeeEfficiencyResult? efficiency = null)
    {
        Success = success;
        Message = message;
        Kpi = kpi;
        Kpis = kpis;
        Efficiency = efficiency;
    }

    public static KpiOperationResult Ok(string message)
    {
        return new KpiOperationResult(true, message);
    }

    public static KpiOperationResult Ok(KpiItem kpi, string message)
    {
        return new KpiOperationResult(true, message, kpi);
    }

    public static KpiOperationResult Ok(IReadOnlyList<KpiItem> kpis, string message)
    {
        return new KpiOperationResult(true, message, kpis: kpis);
    }

    public static KpiOperationResult Ok(EmployeeEfficiencyResult efficiency, string message)
    {
        return new KpiOperationResult(true, message, efficiency: efficiency);
    }

    public static KpiOperationResult Fail(string message)
    {
        return new KpiOperationResult(false, message);
    }
}