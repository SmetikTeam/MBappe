using MBappe.Data;
using MBappe.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public class EfMotivationProgramRepository : IMotivationProgramRepository
{
    private readonly Func<AppDbContext> _dbFactory;

    public EfMotivationProgramRepository(Func<AppDbContext>? dbFactory = null)
    {
        _dbFactory = dbFactory ?? (() => new AppDbContext());
    }
    
    public async Task<MotivationProgram?> GetByIdAsync(Guid id)
    {
        await using var db = _dbFactory();

        return await db.MotivationPrograms
            .AsNoTracking()
            .FirstOrDefaultAsync(program => program.Id == id);
    }

    public async Task<IReadOnlyList<MotivationProgram>> GetAllAsync()
    {
        await using var db = _dbFactory();

        return await db.MotivationPrograms
            .AsNoTracking()
            .OrderByDescending(program => program.IsActive)
            .ThenBy(program => program.Title)
            .ToListAsync();
    }

    public async Task AddAsync(MotivationProgram program)
    {
        await using var db = _dbFactory();

        db.MotivationPrograms.Add(program);

        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(MotivationProgram program)
    {
        await using var db = _dbFactory();

        db.MotivationPrograms.Update(program);

        await db.SaveChangesAsync();
    }
}