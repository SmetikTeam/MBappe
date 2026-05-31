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
    public async Task<MotivationProgram?> GetByIdAsync(Guid id)
    {
        await using var db = new AppDbContext();

        return await db.MotivationPrograms
            .AsNoTracking()
            .FirstOrDefaultAsync(program => program.Id == id);
    }

    public async Task<IReadOnlyList<MotivationProgram>> GetAllAsync()
    {
        await using var db = new AppDbContext();

        return await db.MotivationPrograms
            .AsNoTracking()
            .OrderByDescending(program => program.IsActive)
            .ThenBy(program => program.Title)
            .ToListAsync();
    }

    public async Task AddAsync(MotivationProgram program)
    {
        await using var db = new AppDbContext();

        db.MotivationPrograms.Add(program);

        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(MotivationProgram program)
    {
        await using var db = new AppDbContext();

        db.MotivationPrograms.Update(program);

        await db.SaveChangesAsync();
    }
}