using MBappe.Common;
using MBappe.Models;
using MBappe.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Services;

public class MotivationService
{
    private readonly IMotivationProgramRepository _programRepository;
    private readonly IMotivationBonusRepository _bonusRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly KpiService _kpiService;
    private readonly SessionService _sessionService;
    private readonly AuditLogService _auditLogService;

    public MotivationService(
        IMotivationProgramRepository programRepository,
        IMotivationBonusRepository bonusRepository,
        IEmployeeRepository employeeRepository,
        KpiService kpiService,
        SessionService sessionService,
        AuditLogService auditLogService)
    {
        _programRepository = programRepository;
        _bonusRepository = bonusRepository;
        _employeeRepository = employeeRepository;
        _kpiService = kpiService;
        _sessionService = sessionService;
        _auditLogService = auditLogService;
    }

    public bool CanManageMotivation()
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return false;

        return currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist;
    }

    public bool CanCalculateBonuses()
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return false;

        return currentUser.Role is UserRole.Administrator
            or UserRole.HrSpecialist
            or UserRole.Manager;
    }

    public async Task<MotivationOperationResult> GetProgramsAsync()
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка получить программы мотивации без активной сессии", "Пользователь не авторизован");

        var programs = await _programRepository.GetAllAsync();

        await _auditLogService.LogAsync(
            AuditActionType.MotivationProgramViewed,
            true,
            "Получен список мотивационных программ",
            $"Количество программ: {programs.Count}",
            user: currentUser);

        return MotivationOperationResult.Ok(programs, "Список программ мотивации получен");
    }

    public async Task<MotivationOperationResult> CreateProgramAsync(CreateMotivationProgramRequest request)
    {
        if (!CanManageMotivation())
            return await AccessDeniedAsync("Попытка создать мотивационную программу без прав", "Недостаточно прав для создания программы мотивации");

        var validationError = ValidateCreateProgramRequest(request);

        if (validationError is not null)
            return MotivationOperationResult.Fail(validationError);

        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка создать мотивационную программу без активной сессии", "Пользователь не авторизован");

        var program = new MotivationProgram
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            BaseAmount = request.BaseAmount,
            MinEfficiencyPercent = request.MinEfficiencyPercent,
            MaxEfficiencyPercent = request.MaxEfficiencyPercent,
            IsActive = true,
            CreatedByUserId = currentUser.Id,
            CreatedAt = DateTime.Now
        };

        await _programRepository.AddAsync(program);

        await _auditLogService.LogAsync(
            AuditActionType.MotivationProgramCreated,
            true,
            "Создана мотивационная программа",
            $"Программа: {program.Title}, базовая сумма: {program.BaseAmount}",
            user: currentUser);

        return MotivationOperationResult.Ok(program, "Программа мотивации создана");
    }

    public async Task<MotivationOperationResult> UpdateProgramAsync(UpdateMotivationProgramRequest request)
    {
        if (!CanManageMotivation())
            return await AccessDeniedAsync("Попытка изменить мотивационную программу без прав", "Недостаточно прав для изменения программы мотивации");

        var validationError = ValidateUpdateProgramRequest(request);

        if (validationError is not null)
            return MotivationOperationResult.Fail(validationError);

        var program = await _programRepository.GetByIdAsync(request.ProgramId);

        if (program is null)
            return MotivationOperationResult.Fail("Программа мотивации не найдена");

        program.Title = request.Title.Trim();
        program.Description = request.Description.Trim();
        program.BaseAmount = request.BaseAmount;
        program.MinEfficiencyPercent = request.MinEfficiencyPercent;
        program.MaxEfficiencyPercent = request.MaxEfficiencyPercent;
        program.IsActive = request.IsActive;
        program.UpdatedAt = DateTime.Now;

        await _programRepository.UpdateAsync(program);

        await _auditLogService.LogAsync(
            AuditActionType.MotivationProgramUpdated,
            true,
            "Изменена мотивационная программа",
            $"Программа: {program.Title}, активна: {program.IsActive}",
            user: _sessionService.CurrentUser);

        return MotivationOperationResult.Ok(program, "Программа мотивации обновлена");
    }

    public async Task<MotivationOperationResult> CalculateBonusAsync(CalculateBonusRequest request)
    {
        var validationError = ValidateCalculateBonusRequest(request);

        if (validationError is not null)
            return MotivationOperationResult.Fail(validationError);

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);

        if (employee is null)
            return MotivationOperationResult.Fail("Сотрудник не найден");

        if (!await CanCalculateBonusForEmployeeAsync(employee))
            return await AccessDeniedAsync("Попытка рассчитать бонус без прав", "Недостаточно прав для расчета бонуса сотрудника");

        if (employee.Status == EmployeeStatus.Dismissed)
            return MotivationOperationResult.Fail("Нельзя рассчитать бонус уволенному сотруднику");

        var program = await _programRepository.GetByIdAsync(request.ProgramId);

        if (program is null)
            return MotivationOperationResult.Fail("Программа мотивации не найдена");

        if (!program.IsActive)
            return MotivationOperationResult.Fail("Нельзя рассчитать бонус по неактивной программе");

        var existingBonus = await _bonusRepository.FindExistingAsync(
            request.EmployeeId,
            request.ProgramId,
            request.PeriodStart,
            request.PeriodEnd);

        if (existingBonus is not null)
            return MotivationOperationResult.Fail("Бонус за этот период по выбранной программе уже рассчитан");

        var efficiencyResult = await _kpiService.GetEmployeeEfficiencyAsync(
            request.EmployeeId,
            request.PeriodStart,
            request.PeriodEnd);

        if (!efficiencyResult.Success || efficiencyResult.Efficiency is null)
            return MotivationOperationResult.Fail($"Не удалось рассчитать эффективность: {efficiencyResult.Message}");

        var efficiencyPercent = efficiencyResult.Efficiency.EfficiencyPercent;

        var calculatedAmount = CalculateAmount(
            program.BaseAmount,
            efficiencyPercent,
            program.MinEfficiencyPercent,
            program.MaxEfficiencyPercent);

        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка рассчитать бонус без активной сессии", "Пользователь не авторизован");

        var bonus = new MotivationBonus
        {
            EmployeeId = employee.Id,
            ProgramId = program.Id,
            PeriodStart = request.PeriodStart.Date,
            PeriodEnd = request.PeriodEnd.Date,
            EfficiencyPercent = efficiencyPercent,
            BaseAmount = program.BaseAmount,
            CalculatedAmount = calculatedAmount,
            FinalAmount = calculatedAmount,
            Status = MotivationBonusStatus.PendingApproval,
            Comment = calculatedAmount <= 0
                ? "Бонус не начислен из-за эффективности ниже минимального порога."
                : "Бонус рассчитан автоматически по KPI.",
            CreatedByUserId = currentUser.Id,
            CreatedAt = DateTime.Now
        };

        await _bonusRepository.AddAsync(bonus);

        await _auditLogService.LogAsync(
            AuditActionType.BonusCalculated,
            true,
            "Рассчитан бонус сотрудника",
            $"Сотрудник: {employee.FullName}, программа: {program.Title}, эффективность: {efficiencyPercent}%, сумма: {bonus.FinalAmount}",
            user: currentUser);

        return MotivationOperationResult.Ok(bonus, "Бонус рассчитан");
    }

    public async Task<MotivationOperationResult> GetVisibleBonusesAsync()
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка получить бонусы без активной сессии", "Пользователь не авторизован");

        var allBonuses = await _bonusRepository.GetAllAsync();

        IReadOnlyList<MotivationBonus> visibleBonuses;

        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
        {
            visibleBonuses = allBonuses;
        }
        else
        {
            var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

            if (currentEmployee is null)
                return MotivationOperationResult.Fail("Для текущей учетной записи не создан профиль сотрудника");

            if (currentUser.Role == UserRole.Manager)
            {
                var employees = await _employeeRepository.GetAllAsync();

                var visibleEmployeeIds = employees
                    .Where(employee => employee.Id == currentEmployee.Id || employee.ManagerEmployeeId == currentEmployee.Id)
                    .Select(employee => employee.Id)
                    .ToHashSet();

                visibleBonuses = allBonuses
                    .Where(bonus => visibleEmployeeIds.Contains(bonus.EmployeeId))
                    .ToList();
            }
            else
            {
                visibleBonuses = allBonuses
                    .Where(bonus => bonus.EmployeeId == currentEmployee.Id)
                    .ToList();
            }
        }

        await _auditLogService.LogAsync(
            AuditActionType.BonusViewed,
            true,
            "Получен список видимых бонусов",
            $"Количество бонусов: {visibleBonuses.Count}",
            user: currentUser);

        return MotivationOperationResult.Ok(visibleBonuses, "Список бонусов получен");
    }

    public async Task<MotivationOperationResult> GetEmployeeBonusesAsync(Guid employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee is null)
            return MotivationOperationResult.Fail("Сотрудник не найден");

        if (!await CanViewBonusForEmployeeAsync(employee))
            return await AccessDeniedAsync("Попытка получить бонусы сотрудника без прав", "Недостаточно прав для просмотра бонусов сотрудника");

        var bonuses = await _bonusRepository.GetByEmployeeIdAsync(employeeId);

        await _auditLogService.LogAsync(
            AuditActionType.BonusViewed,
            true,
            "Получены бонусы сотрудника",
            $"Сотрудник: {employee.FullName}, количество бонусов: {bonuses.Count}",
            user: _sessionService.CurrentUser);

        return MotivationOperationResult.Ok(bonuses, "Бонусы сотрудника получены");
    }

    public async Task<MotivationOperationResult> ApproveBonusAsync(Guid bonusId)
    {
        if (!CanManageMotivation())
            return await AccessDeniedAsync("Попытка утвердить бонус без прав", "Недостаточно прав для утверждения бонуса");

        var bonus = await _bonusRepository.GetByIdAsync(bonusId);

        if (bonus is null)
            return MotivationOperationResult.Fail("Бонус не найден");

        if (bonus.Status != MotivationBonusStatus.PendingApproval)
            return MotivationOperationResult.Fail("Утвердить можно только бонус, ожидающий утверждения");

        bonus.Status = MotivationBonusStatus.Approved;
        bonus.ApprovedByUserId = _sessionService.CurrentUser?.Id;
        bonus.ApprovedAt = DateTime.Now;
        bonus.UpdatedAt = DateTime.Now;

        await _bonusRepository.UpdateAsync(bonus);

        await _auditLogService.LogAsync(
            AuditActionType.BonusApproved,
            true,
            "Бонус утвержден",
            $"Бонус: {bonus.Id}, сумма: {bonus.FinalAmount}",
            user: _sessionService.CurrentUser);

        return MotivationOperationResult.Ok(bonus, "Бонус утвержден");
    }

    public async Task<MotivationOperationResult> RejectBonusAsync(Guid bonusId, string comment)
    {
        if (!CanManageMotivation())
            return await AccessDeniedAsync("Попытка отклонить бонус без прав", "Недостаточно прав для отклонения бонуса");

        var bonus = await _bonusRepository.GetByIdAsync(bonusId);

        if (bonus is null)
            return MotivationOperationResult.Fail("Бонус не найден");

        if (bonus.Status != MotivationBonusStatus.PendingApproval)
            return MotivationOperationResult.Fail("Отклонить можно только бонус, ожидающий утверждения");

        bonus.Status = MotivationBonusStatus.Rejected;
        bonus.RejectedByUserId = _sessionService.CurrentUser?.Id;
        bonus.RejectedAt = DateTime.Now;
        bonus.UpdatedAt = DateTime.Now;
        bonus.Comment = string.IsNullOrWhiteSpace(comment)
            ? "Бонус отклонен."
            : comment.Trim();

        await _bonusRepository.UpdateAsync(bonus);

        await _auditLogService.LogAsync(
            AuditActionType.BonusRejected,
            true,
            "Бонус отклонен",
            $"Бонус: {bonus.Id}, комментарий: {bonus.Comment}",
            user: _sessionService.CurrentUser);

        return MotivationOperationResult.Ok(bonus, "Бонус отклонен");
    }

    public async Task<MotivationOperationResult> MarkBonusAsPaidAsync(Guid bonusId)
    {
        if (!CanManageMotivation())
            return await AccessDeniedAsync("Попытка отметить бонус выплаченным без прав", "Недостаточно прав для отметки бонуса выплаченным");

        var bonus = await _bonusRepository.GetByIdAsync(bonusId);

        if (bonus is null)
            return MotivationOperationResult.Fail("Бонус не найден");

        if (bonus.Status != MotivationBonusStatus.Approved)
            return MotivationOperationResult.Fail("Выплатить можно только утвержденный бонус");

        bonus.Status = MotivationBonusStatus.Paid;
        bonus.PaidByUserId = _sessionService.CurrentUser?.Id;
        bonus.PaidAt = DateTime.Now;
        bonus.UpdatedAt = DateTime.Now;

        await _bonusRepository.UpdateAsync(bonus);

        await _auditLogService.LogAsync(
            AuditActionType.BonusPaid,
            true,
            "Бонус отмечен как выплаченный",
            $"Бонус: {bonus.Id}, сумма: {bonus.FinalAmount}",
            user: _sessionService.CurrentUser);

        return MotivationOperationResult.Ok(bonus, "Бонус отмечен как выплаченный");
    }

    public async Task<MotivationOperationResult> CancelBonusAsync(Guid bonusId)
    {
        if (!CanManageMotivation())
            return await AccessDeniedAsync("Попытка отменить бонус без прав", "Недостаточно прав для отмены бонуса");

        var bonus = await _bonusRepository.GetByIdAsync(bonusId);

        if (bonus is null)
            return MotivationOperationResult.Fail("Бонус не найден");

        if (bonus.Status == MotivationBonusStatus.Paid)
            return MotivationOperationResult.Fail("Нельзя отменить уже выплаченный бонус");

        if (bonus.Status == MotivationBonusStatus.Cancelled)
            return MotivationOperationResult.Fail("Бонус уже отменен");

        bonus.Status = MotivationBonusStatus.Cancelled;
        bonus.CancelledByUserId = _sessionService.CurrentUser?.Id;
        bonus.CancelledAt = DateTime.Now;
        bonus.UpdatedAt = DateTime.Now;
        bonus.Comment = "Бонус отменен.";

        await _bonusRepository.UpdateAsync(bonus);

        await _auditLogService.LogAsync(
            AuditActionType.BonusCancelled,
            true,
            "Бонус отменен",
            $"Бонус: {bonus.Id}",
            user: _sessionService.CurrentUser);

        return MotivationOperationResult.Ok(bonus, "Бонус отменен");
    }

    private async Task<bool> CanCalculateBonusForEmployeeAsync(EmployeeProfile employee)
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return false;

        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            return true;

        if (currentUser.Role != UserRole.Manager)
            return false;

        var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

        if (currentEmployee is null)
            return false;

        return employee.ManagerEmployeeId == currentEmployee.Id;
    }

    private async Task<bool> CanViewBonusForEmployeeAsync(EmployeeProfile employee)
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return false;

        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
            return true;

        var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

        if (currentEmployee is null)
            return false;

        if (employee.Id == currentEmployee.Id)
            return true;

        if (currentUser.Role == UserRole.Manager && employee.ManagerEmployeeId == currentEmployee.Id)
            return true;

        return false;
    }

    private static decimal CalculateAmount(
        decimal baseAmount,
        double efficiencyPercent,
        double minEfficiencyPercent,
        double maxEfficiencyPercent)
    {
        if (efficiencyPercent < minEfficiencyPercent)
            return 0m;

        var cappedEfficiency = Math.Min(efficiencyPercent, maxEfficiencyPercent);
        var coefficient = (decimal)(cappedEfficiency / 100d);

        return Math.Round(baseAmount * coefficient, 2);
    }

    private static string? ValidateCreateProgramRequest(CreateMotivationProgramRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return "Введите название программы мотивации";

        if (request.Title.Trim().Length < 3)
            return "Название программы должно содержать минимум 3 символа";

        if (request.BaseAmount <= 0)
            return "Базовая сумма должна быть больше нуля";

        if (request.MinEfficiencyPercent < 0 || request.MinEfficiencyPercent > 100)
            return "Минимальная эффективность должна быть от 0 до 100";

        if (request.MaxEfficiencyPercent < 100 || request.MaxEfficiencyPercent > 200)
            return "Максимальная эффективность должна быть от 100 до 200";

        if (request.MaxEfficiencyPercent < request.MinEfficiencyPercent)
            return "Максимальная эффективность не может быть меньше минимальной";

        return null;
    }

    private static string? ValidateUpdateProgramRequest(UpdateMotivationProgramRequest request)
    {
        if (request.ProgramId == Guid.Empty)
            return "Не выбрана программа мотивации";

        if (string.IsNullOrWhiteSpace(request.Title))
            return "Введите название программы мотивации";

        if (request.Title.Trim().Length < 3)
            return "Название программы должно содержать минимум 3 символа";

        if (request.BaseAmount <= 0)
            return "Базовая сумма должна быть больше нуля";

        if (request.MinEfficiencyPercent < 0 || request.MinEfficiencyPercent > 100)
            return "Минимальная эффективность должна быть от 0 до 100";

        if (request.MaxEfficiencyPercent < 100 || request.MaxEfficiencyPercent > 200)
            return "Максимальная эффективность должна быть от 100 до 200";

        if (request.MaxEfficiencyPercent < request.MinEfficiencyPercent)
            return "Максимальная эффективность не может быть меньше минимальной";

        return null;
    }

    private static string? ValidateCalculateBonusRequest(CalculateBonusRequest request)
    {
        if (request.EmployeeId == Guid.Empty)
            return "Выберите сотрудника";

        if (request.ProgramId == Guid.Empty)
            return "Выберите программу мотивации";

        if (request.PeriodEnd.Date < request.PeriodStart.Date)
            return "Дата окончания периода не может быть раньше даты начала";

        return null;
    }

    private async Task<MotivationOperationResult> AccessDeniedAsync(string details, string message)
    {
        await _auditLogService.LogAsync(
            AuditActionType.AccessDenied,
            false,
            "Отказано в доступе",
            details,
            user: _sessionService.CurrentUser);

        return MotivationOperationResult.Fail(message);
    }
}