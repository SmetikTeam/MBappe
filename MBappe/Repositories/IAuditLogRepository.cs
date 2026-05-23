using MBappe.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry);

    Task<IReadOnlyList<AuditLogEntry>> GetAllAsync();

    Task<IReadOnlyList<AuditLogEntry>> GetByUserLoginAsync(string login);

    Task<IReadOnlyList<AuditLogEntry>> GetByActionTypeAsync(AuditActionType actionType);
}