using MBappe.Models;
using MBappe.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MBappe.Services;

public class AuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly SessionService _sessionService;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        SessionService sessionService)
    {
        _auditLogRepository = auditLogRepository;
        _sessionService = sessionService;
    }

    public async Task LogAsync(
        AuditActionType actionType,
        bool isSuccess,
        string message,
        string? details = null,
        AppUser? user = null,
        string? login = null)
    {
        var currentUser = user ?? _sessionService.CurrentUser;

        var entry = new AuditLogEntry
        {
            UserId = currentUser?.Id,
            UserLogin = currentUser?.Login ?? login,
            UserRole = currentUser?.Role,
            ActionType = actionType,
            IsSuccess = isSuccess,
            Message = message,
            Details = details
        };

        await _auditLogRepository.AddAsync(entry);
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetAllAsync()
    {
        return _auditLogRepository.GetAllAsync();
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetByUserLoginAsync(string login)
    {
        return _auditLogRepository.GetByUserLoginAsync(login);
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetByActionTypeAsync(AuditActionType actionType)
    {
        return _auditLogRepository.GetByActionTypeAsync(actionType);
    }
}