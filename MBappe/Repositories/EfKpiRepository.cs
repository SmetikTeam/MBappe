using MBappe.Data;
using MBappe.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public class EfKpiRepository : IKpiRepository
{
    public async Task<KpiItem?> GetByIdAsync(Guid id)
    {
        await using var db = new AppDbContext();

        return await db.Kpis
            .AsNoTracking()
            .FirstOrDefaultAsync(kpi => kpi.Id == id);
    }

    public async Task<IReadOnlyList<KpiItem>> GetAllAsync()
    {
        await using var db = new AppDbContext();

        return await db.Kpis
            .AsNoTracking()
            .OrderByDescending(kpi => kpi.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<KpiItem>> GetByEmployeeIdAsync(Guid employeeId)
    {
        await using var db = new AppDbContext();

        return await db.Kpis
            .AsNoTracking()
            .Where(kpi => kpi.EmployeeId == employeeId)
            .OrderByDescending(kpi => kpi.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<KpiItem>> GetByPeriodAsync(DateTime periodStart, DateTime periodEnd)
    {
        await using var db = new AppDbContext();

        var start = periodStart.Date;
        var end = periodEnd.Date;

        return await db.Kpis
            .AsNoTracking()
            .Where(kpi => kpi.PeriodStart.Date <= end && kpi.PeriodEnd.Date >= start)
            .OrderByDescending(kpi => kpi.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(KpiItem kpi)
    {
        await using var db = new AppDbContext();

        db.Kpis.Add(kpi);

        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(KpiItem kpi)
    {
        await using var db = new AppDbContext();

        db.Kpis.Update(kpi);

        await db.SaveChangesAsync();
    }
}