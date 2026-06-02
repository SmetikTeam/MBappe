using MBappe.Data;
using MBappe.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public class EfMotivationBonusRepository : IMotivationBonusRepository
{
    private readonly Func<AppDbContext> _dbFactory;

    public EfMotivationBonusRepository(Func<AppDbContext>? dbFactory = null)
    {
        _dbFactory = dbFactory ?? (() => new AppDbContext());
    }
    
    public async Task<MotivationBonus?> GetByIdAsync(Guid id)
    {
        await using var db = _dbFactory();

        return await db.MotivationBonuses
            .AsNoTracking()
            .FirstOrDefaultAsync(bonus => bonus.Id == id);
    }

    public async Task<IReadOnlyList<MotivationBonus>> GetAllAsync()
    {
        await using var db = _dbFactory();

        return await db.MotivationBonuses
            .AsNoTracking()
            .OrderByDescending(bonus => bonus.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MotivationBonus>> GetByEmployeeIdAsync(Guid employeeId)
    {
        await using var db = _dbFactory();

        return await db.MotivationBonuses
            .AsNoTracking()
            .Where(bonus => bonus.EmployeeId == employeeId)
            .OrderByDescending(bonus => bonus.CreatedAt)
            .ToListAsync();
    }

    public async Task<MotivationBonus?> FindExistingAsync(
        Guid employeeId,
        Guid programId,
        DateTime periodStart,
        DateTime periodEnd)
    {
        await using var db = _dbFactory();

        var start = periodStart.Date;
        var end = periodEnd.Date;

        return await db.MotivationBonuses
            .AsNoTracking()
            .FirstOrDefaultAsync(bonus =>
                bonus.EmployeeId == employeeId
                && bonus.ProgramId == programId
                && bonus.PeriodStart.Date == start
                && bonus.PeriodEnd.Date == end
                && bonus.Status != MotivationBonusStatus.Cancelled);
    }

    public async Task AddAsync(MotivationBonus bonus)
    {
        await using var db = _dbFactory();

        db.MotivationBonuses.Add(bonus);

        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(MotivationBonus bonus)
    {
        await using var db = _dbFactory();

        db.MotivationBonuses.Update(bonus);

        await db.SaveChangesAsync();
    }
}