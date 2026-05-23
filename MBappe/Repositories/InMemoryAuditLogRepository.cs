using MBappe.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace MBappe.Repositories;

public class InMemoryAuditLogRepository : IAuditLogRepository
{
    private readonly List<AuditLogEntry> _entries = [];

    public Task AddAsync(AuditLogEntry entry)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetAllAsync()
    {
        var entries = _entries
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<AuditLogEntry>>(entries);
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetByUserLoginAsync(string login)
    {
        var entries = _entries
            .Where(e => string.Equals(e.UserLogin, login, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<AuditLogEntry>>(entries);
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetByActionTypeAsync(AuditActionType actionType)
    {
        var entries = _entries
            .Where(e => e.ActionType == actionType)
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<AuditLogEntry>>(entries);
    }
}