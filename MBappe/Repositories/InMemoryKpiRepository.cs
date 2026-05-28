using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace MBappe.Repositories;

public class InMemoryKpiRepository : IKpiRepository
{
    private readonly List<KpiItem> _kpis = [];

    public Task<KpiItem?> GetByIdAsync(Guid id)
    {
        var kpi = _kpis.FirstOrDefault(k => k.Id == id);

        return Task.FromResult(kpi);
    }

    public Task<IReadOnlyList<KpiItem>> GetAllAsync()
    {
        var kpis = _kpis
            .OrderByDescending(k => k.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<KpiItem>>(kpis);
    }

    public Task<IReadOnlyList<KpiItem>> GetByEmployeeIdAsync(Guid employeeId)
    {
        var kpis = _kpis
            .Where(k => k.EmployeeId == employeeId)
            .OrderByDescending(k => k.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<KpiItem>>(kpis);
    }

    public Task<IReadOnlyList<KpiItem>> GetByPeriodAsync(DateTime periodStart, DateTime periodEnd)
    {
        var start = periodStart.Date;
        var end = periodEnd.Date;

        var kpis = _kpis
            .Where(k => k.PeriodStart.Date <= end && k.PeriodEnd.Date >= start)
            .OrderByDescending(k => k.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<KpiItem>>(kpis);
    }

    public Task AddAsync(KpiItem kpi)
    {
        _kpis.Add(kpi);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(KpiItem kpi)
    {
        return Task.CompletedTask;
    }
}