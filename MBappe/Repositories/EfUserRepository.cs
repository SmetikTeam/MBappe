using MBappe.Data;
using MBappe.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public class EfUserRepository : IUserRepository
{
    public async Task<AppUser?> GetByIdAsync(Guid id)
    {
        await using var db = new AppDbContext();

        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task<AppUser?> GetByLoginAsync(string login)
    {
        await using var db = new AppDbContext();

        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Login.ToLower() == login.ToLower());
    }

    public async Task<AppUser?> GetByEmailAsync(string email)
    {
        await using var db = new AppDbContext();

        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email.ToLower() == email.ToLower());
    }

    public async Task AddAsync(AppUser user)
    {
        await using var db = new AppDbContext();

        db.Users.Add(user);

        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppUser user)
    {
        await using var db = new AppDbContext();

        db.Users.Update(user);

        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AppUser>> GetAllAsync()
    {
        await using var db = new AppDbContext();

        return await db.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .ToListAsync();
    }
}