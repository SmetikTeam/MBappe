using MBappe.Data;
using MBappe.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public class EfAuditLogRepository : IAuditLogRepository
{
    public async Task AddAsync(AuditLogEntry entry)
    {
        await using var db = new AppDbContext();

        db.AuditLogs.Add(entry);

        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetAllAsync()
    {
        await using var db = new AppDbContext();

        return await db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetByUserLoginAsync(string login)
    {
        await using var db = new AppDbContext();

        return await db.AuditLogs
            .AsNoTracking()
            .Where(entry => entry.UserLogin != null && entry.UserLogin.ToLower() == login.ToLower())
            .OrderByDescending(entry => entry.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetByActionTypeAsync(AuditActionType actionType)
    {
        await using var db = new AppDbContext();

        return await db.AuditLogs
            .AsNoTracking()
            .Where(entry => entry.ActionType == actionType)
            .OrderByDescending(entry => entry.CreatedAt)
            .ToListAsync();
    }
}