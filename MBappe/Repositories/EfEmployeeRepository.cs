using MBappe.Data;
using MBappe.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public class EfEmployeeRepository : IEmployeeRepository
{
    private readonly Func<AppDbContext> _dbFactory;

    public EfEmployeeRepository(Func<AppDbContext>? dbFactory = null)
    {
        _dbFactory = dbFactory ?? (() => new AppDbContext());
    }
    
    public async Task<EmployeeProfile?> GetByIdAsync(Guid id)
    {
        await using var db = _dbFactory();

        return await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(employee => employee.Id == id);
    }

    public async Task<EmployeeProfile?> GetByUserIdAsync(Guid userId)
    {
        await using var db = _dbFactory();

        return await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(employee => employee.UserId == userId);
    }

    public async Task<EmployeeProfile?> GetByPersonnelNumberAsync(string personnelNumber)
    {
        await using var db = _dbFactory();

        return await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(employee =>
                employee.PersonnelNumber.ToLower() == personnelNumber.ToLower());
    }

    public async Task<IReadOnlyList<EmployeeProfile>> GetAllAsync()
    {
        await using var db = _dbFactory();

        return await db.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.FullName)
            .ToListAsync();
    }

    public async Task AddAsync(EmployeeProfile employee)
    {
        await using var db = _dbFactory();

        db.Employees.Add(employee);

        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmployeeProfile employee)
    {
        await using var db = _dbFactory();

        db.Employees.Update(employee);

        await db.SaveChangesAsync();
    }
}