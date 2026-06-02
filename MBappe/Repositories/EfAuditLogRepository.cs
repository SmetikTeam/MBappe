using MBappe.Data;
using MBappe.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MBappe.Repositories;

public class EfAuditLogRepository : IAuditLogRepository
{
    private readonly Func<AppDbContext> _dbFactory;

    public EfAuditLogRepository(Func<AppDbContext>? dbFactory = null)
    {
        _dbFactory = dbFactory ?? (() => new AppDbContext());
    }
    
    public async Task AddAsync(AuditLogEntry entry)
    {
        await using var db = _dbFactory();

        db.AuditLogs.Add(entry);

        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetAllAsync()
    {
        await using var db = _dbFactory();

        return await db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetByUserLoginAsync(string login)
    {
        await using var db = _dbFactory();

        return await db.AuditLogs
            .AsNoTracking()
            .Where(entry => entry.UserLogin != null && entry.UserLogin.ToLower() == login.ToLower())
            .OrderByDescending(entry => entry.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetByActionTypeAsync(AuditActionType actionType)
    {
        await using var db = _dbFactory();

        return await db.AuditLogs
            .AsNoTracking()
            .Where(entry => entry.ActionType == actionType)
            .OrderByDescending(entry => entry.CreatedAt)
            .ToListAsync();
    }
}