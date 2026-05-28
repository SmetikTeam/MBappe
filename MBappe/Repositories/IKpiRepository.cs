using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public interface IKpiRepository
{
    Task<KpiItem?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<KpiItem>> GetAllAsync();

    Task<IReadOnlyList<KpiItem>> GetByEmployeeIdAsync(Guid employeeId);

    Task<IReadOnlyList<KpiItem>> GetByPeriodAsync(DateTime periodStart, DateTime periodEnd);

    Task AddAsync(KpiItem kpi);

    Task UpdateAsync(KpiItem kpi);
}