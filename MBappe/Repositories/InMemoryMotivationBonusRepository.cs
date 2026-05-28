using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace MBappe.Repositories;

public class InMemoryMotivationBonusRepository : IMotivationBonusRepository
{
    private readonly List<MotivationBonus> _bonuses = [];

    public Task<MotivationBonus?> GetByIdAsync(Guid id)
    {
        var bonus = _bonuses.FirstOrDefault(bonus => bonus.Id == id);

        return Task.FromResult(bonus);
    }

    public Task<IReadOnlyList<MotivationBonus>> GetAllAsync()
    {
        var bonuses = _bonuses
            .OrderByDescending(bonus => bonus.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<MotivationBonus>>(bonuses);
    }

    public Task<IReadOnlyList<MotivationBonus>> GetByEmployeeIdAsync(Guid employeeId)
    {
        var bonuses = _bonuses
            .Where(bonus => bonus.EmployeeId == employeeId)
            .OrderByDescending(bonus => bonus.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<MotivationBonus>>(bonuses);
    }

    public Task<MotivationBonus?> FindExistingAsync(
        Guid employeeId,
        Guid programId,
        DateTime periodStart,
        DateTime periodEnd)
    {
        var start = periodStart.Date;
        var end = periodEnd.Date;

        var bonus = _bonuses.FirstOrDefault(bonus =>
            bonus.EmployeeId == employeeId
            && bonus.ProgramId == programId
            && bonus.PeriodStart.Date == start
            && bonus.PeriodEnd.Date == end
            && bonus.Status != MotivationBonusStatus.Cancelled);

        return Task.FromResult(bonus);
    }

    public Task AddAsync(MotivationBonus bonus)
    {
        _bonuses.Add(bonus);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(MotivationBonus bonus)
    {
        return Task.CompletedTask;
    }
}