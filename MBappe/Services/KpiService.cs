using MBappe.Common;
using MBappe.Models;
using MBappe.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Services;

public class KpiService
{
    private const double MaxBonusCompletionPercent = 120;

    private readonly IKpiRepository _kpiRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly SessionService _sessionService;
    private readonly AuditLogService _auditLogService;

    public KpiService(
        IKpiRepository kpiRepository,
        IEmployeeRepository employeeRepository,
        SessionService sessionService,
        AuditLogService auditLogService)
    {
        _kpiRepository = kpiRepository;
        _employeeRepository = employeeRepository;
        _sessionService = sessionService;
        _auditLogService = auditLogService;
    }

    public async Task<KpiOperationResult> GetVisibleKpisAsync()
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка получить KPI без активной сессии", "Пользователь не авторизован");

        var allKpis = await _kpiRepository.GetAllAsync();
        IReadOnlyList<KpiItem> visibleKpis;

        if (currentUser.Role is UserRole.Administrator or UserRole.HrSpecialist)
        {
            visibleKpis = allKpis;
        }
        else
        {
            var currentEmployee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

            if (currentEmployee is null)
                return KpiOperationResult.Fail("Для текущей учетной записи не создан профиль сотрудника");

            if (currentUser.Role == UserRole.Manager)
            {
                var allEmployees = await _employeeRepository.GetAllAsync();

                var visibleEmployeeIds = allEmployees
                    .Where(employee => employee.Id == currentEmployee.Id || employee.ManagerEmployeeId == currentEmployee.Id)
                    .Select(employee => employee.Id)
                    .ToHashSet();

                visibleKpis = allKpis
                    .Where(kpi => visibleEmployeeIds.Contains(kpi.EmployeeId))
                    .ToList();
            }
            else
            {
                visibleKpis = allKpis
                    .Where(kpi => kpi.EmployeeId == currentEmployee.Id)
                    .ToList();
            }
        }

        await _auditLogService.LogAsync(
            AuditActionType.KpiViewed,
            true,
            "Получен список видимых KPI",
            $"Количество KPI: {visibleKpis.Count}");

        return KpiOperationResult.Ok(visibleKpis, "Список KPI получен");
    }

    public async Task<KpiOperationResult> GetEmployeeKpisAsync(Guid employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee is null)
            return KpiOperationResult.Fail("Сотрудник не найден");

        if (!await CanViewEmployeeKpisAsync(employee))
            return await AccessDeniedAsync("Попытка получить KPI сотрудника без прав", "Недостаточно прав для просмотра KPI сотрудника");

        var kpis = await _kpiRepository.GetByEmployeeIdAsync(employeeId);

        await _auditLogService.LogAsync(
            AuditActionType.KpiViewed,
            true,
            "Получены KPI сотрудника",
            $"Сотрудник: {employee.FullName}, количество KPI: {kpis.Count}");

        return KpiOperationResult.Ok(kpis, "KPI сотрудника получены");
    }

    public async Task<KpiOperationResult> CreateKpiAsync(CreateKpiRequest request)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);

        if (employee is null)
            return KpiOperationResult.Fail("Сотрудник не найден");

        if (!await CanEditEmployeeKpisAsync(employee))
            return await AccessDeniedAsync("Попытка создать KPI без прав", "Недостаточно прав для создания KPI");

        var validationError = ValidateCreateRequest(request);

        if (validationError is not null)
            return KpiOperationResult.Fail(validationError);

        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка создать KPI без активной сессии", "Пользователь не авторизован");

        var kpi = new KpiItem
        {
            EmployeeId = request.EmployeeId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            TargetValue = request.TargetValue,
            ActualValue = request.ActualValue,
            Unit = request.Unit.Trim(),
            WeightPercent = request.WeightPercent,
            PeriodStart = request.PeriodStart.Date,
            PeriodEnd = request.PeriodEnd.Date,
            CreatedByUserId = currentUser.Id,
            CreatedAt = DateTime.Now
        };

        RecalculateStatus(kpi);

        await _kpiRepository.AddAsync(kpi);

        await _auditLogService.LogAsync(
            AuditActionType.KpiCreated,
            true,
            "Создан KPI",
            $"Сотрудник: {employee.FullName}, KPI: {kpi.Title}, план: {kpi.TargetValue}, факт: {kpi.ActualValue}");

        return KpiOperationResult.Ok(kpi, "KPI успешно создан");
    }

    public async Task<KpiOperationResult> UpdateKpiAsync(UpdateKpiRequest request)
    {
        var kpi = await _kpiRepository.GetByIdAsync(request.KpiId);

        if (kpi is null)
            return KpiOperationResult.Fail("KPI не найден");

        if (kpi.Status == KpiStatus.Cancelled)
            return KpiOperationResult.Fail("Нельзя редактировать отмененный KPI");

        var employee = await _employeeRepository.GetByIdAsync(kpi.EmployeeId);

        if (employee is null)
            return KpiOperationResult.Fail("Сотрудник, которому назначен KPI, не найден");

        if (!await CanEditEmployeeKpisAsync(employee))
            return await AccessDeniedAsync("Попытка изменить KPI без прав", "Недостаточно прав для изменения KPI");

        var validationError = ValidateUpdateRequest(request);

        if (validationError is not null)
            return KpiOperationResult.Fail(validationError);

        kpi.Title = request.Title.Trim();
        kpi.Description = request.Description.Trim();
        kpi.TargetValue = request.TargetValue;
        kpi.Unit = request.Unit.Trim();
        kpi.WeightPercent = request.WeightPercent;
        kpi.PeriodStart = request.PeriodStart.Date;
        kpi.PeriodEnd = request.PeriodEnd.Date;
        kpi.UpdatedAt = DateTime.Now;

        RecalculateStatus(kpi);

        await _kpiRepository.UpdateAsync(kpi);

        await _auditLogService.LogAsync(
            AuditActionType.KpiUpdated,
            true,
            "Изменен KPI",
            $"Сотрудник: {employee.FullName}, KPI: {kpi.Title}");

        return KpiOperationResult.Ok(kpi, "KPI обновлен");
    }

    public async Task<KpiOperationResult> UpdateKpiProgressAsync(UpdateKpiProgressRequest request)
    {
        var kpi = await _kpiRepository.GetByIdAsync(request.KpiId);

        if (kpi is null)
            return KpiOperationResult.Fail("KPI не найден");

        if (kpi.Status == KpiStatus.Cancelled)
            return KpiOperationResult.Fail("Нельзя обновлять прогресс отмененного KPI");

        if (request.ActualValue < 0)
            return KpiOperationResult.Fail("Фактическое значение не может быть отрицательным");

        var employee = await _employeeRepository.GetByIdAsync(kpi.EmployeeId);

        if (employee is null)
            return KpiOperationResult.Fail("Сотрудник, которому назначен KPI, не найден");

        if (!await CanEditEmployeeKpisAsync(employee))
            return await AccessDeniedAsync("Попытка обновить прогресс KPI без прав", "Недостаточно прав для обновления KPI");

        var oldValue = kpi.ActualValue;
        kpi.ActualValue = request.ActualValue;
        kpi.UpdatedAt = DateTime.Now;

        RecalculateStatus(kpi);

        await _kpiRepository.UpdateAsync(kpi);

        await _auditLogService.LogAsync(
            AuditActionType.KpiProgressUpdated,
            true,
            "Обновлен прогресс KPI",
            $"KPI: {kpi.Title}, старый факт: {oldValue}, новый факт: {kpi.ActualValue}, выполнение: {kpi.CompletionPercent}%");

        return KpiOperationResult.Ok(kpi, "Прогресс KPI обновлен");
    }

    public async Task<KpiOperationResult> CancelKpiAsync(Guid kpiId)
    {
        var kpi = await _kpiRepository.GetByIdAsync(kpiId);

        if (kpi is null)
            return KpiOperationResult.Fail("KPI не найден");

        if (kpi.Status == KpiStatus.Cancelled)
            return KpiOperationResult.Fail("KPI уже отменен");

        var employee = await _employeeRepository.GetByIdAsync(kpi.EmployeeId);

        if (employee is null)
            return KpiOperationResult.Fail("Сотрудник, которому назначен KPI, не найден");

        if (!await CanEditEmployeeKpisAsync(employee))
            return await AccessDeniedAsync("Попытка отменить KPI без прав", "Недостаточно прав для отмены KPI");

        kpi.Status = KpiStatus.Cancelled;
        kpi.UpdatedAt = DateTime.Now;

        await _kpiRepository.UpdateAsync(kpi);

        await _auditLogService.LogAsync(
            AuditActionType.KpiCancelled,
            true,
            "KPI отменен",
            $"Сотрудник: {employee.FullName}, KPI: {kpi.Title}");

        return KpiOperationResult.Ok(kpi, "KPI отменен");
    }

    public async Task<KpiOperationResult> GetEmployeeEfficiencyAsync(
        Guid employeeId,
        DateTime periodStart,
        DateTime periodEnd)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee is null)
            return KpiOperationResult.Fail("Сотрудник не найден");

        if (!await CanViewEmployeeKpisAsync(employee))
            return await AccessDeniedAsync("Попытка рассчитать эффективность без прав", "Недостаточно прав для расчета эффективности");

        if (periodEnd.Date < periodStart.Date)
            return KpiOperationResult.Fail("Дата окончания периода не может быть раньше даты начала");

        var employeeKpis = await _kpiRepository.GetByEmployeeIdAsync(employeeId);

        var periodKpis = employeeKpis
            .Where(kpi => kpi.Status != KpiStatus.Cancelled)
            .Where(kpi => kpi.PeriodStart.Date <= periodEnd.Date && kpi.PeriodEnd.Date >= periodStart.Date)
            .ToList();

        if (periodKpis.Count == 0)
        {
            var emptyEfficiency = new EmployeeEfficiencyResult(
                employeeId,
                periodStart.Date,
                periodEnd.Date,
                0,
                0,
                0,
                periodKpis);

            return KpiOperationResult.Ok(emptyEfficiency, "За выбранный период KPI не найдены");
        }

        var totalWeight = periodKpis.Sum(kpi => kpi.WeightPercent);

        if (totalWeight <= 0)
            return KpiOperationResult.Fail("Суммарный вес KPI должен быть больше нуля");

        var efficiency = periodKpis.Sum(kpi =>
        {
            var cappedPercent = Math.Min(kpi.CompletionPercent, MaxBonusCompletionPercent);
            return cappedPercent * kpi.WeightPercent / totalWeight;
        });

        var result = new EmployeeEfficiencyResult(
            employeeId,
            periodStart.Date,
            periodEnd.Date,
            periodKpis.Count,
            Math.Round(totalWeight, 2),
            Math.Round(efficiency, 2),
            periodKpis);

        await _auditLogService.LogAsync(
            AuditActionType.KpiEfficiencyCalculated,
            true,
            "Рассчитана эффективность сотрудника",
            $"Сотрудник: {employee.FullName}, эффективность: {result.EfficiencyPercent}%, KPI: {result.KpiCount}");

        return KpiOperationResult.Ok(result, "Эффективность сотрудника рассчитана");
    }

    private async Task<bool> CanViewEmployeeKpisAsync(EmployeeProfile employee)
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

    private async Task<bool> CanEditEmployeeKpisAsync(EmployeeProfile employee)
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

    private static string? ValidateCreateRequest(CreateKpiRequest request)
    {
        if (request.EmployeeId == Guid.Empty)
            return "Выберите сотрудника";

        if (string.IsNullOrWhiteSpace(request.Title))
            return "Введите название KPI";

        if (request.Title.Trim().Length < 3)
            return "Название KPI должно содержать минимум 3 символа";

        if (request.TargetValue <= 0)
            return "Плановое значение должно быть больше нуля";

        if (request.ActualValue < 0)
            return "Фактическое значение не может быть отрицательным";

        if (string.IsNullOrWhiteSpace(request.Unit))
            return "Введите единицу измерения";

        if (request.WeightPercent <= 0 || request.WeightPercent > 100)
            return "Вес KPI должен быть от 1 до 100";

        if (request.PeriodEnd.Date < request.PeriodStart.Date)
            return "Дата окончания периода не может быть раньше даты начала";

        return null;
    }

    private static string? ValidateUpdateRequest(UpdateKpiRequest request)
    {
        if (request.KpiId == Guid.Empty)
            return "Не выбран KPI";

        if (string.IsNullOrWhiteSpace(request.Title))
            return "Введите название KPI";

        if (request.Title.Trim().Length < 3)
            return "Название KPI должно содержать минимум 3 символа";

        if (request.TargetValue <= 0)
            return "Плановое значение должно быть больше нуля";

        if (string.IsNullOrWhiteSpace(request.Unit))
            return "Введите единицу измерения";

        if (request.WeightPercent <= 0 || request.WeightPercent > 100)
            return "Вес KPI должен быть от 1 до 100";

        if (request.PeriodEnd.Date < request.PeriodStart.Date)
            return "Дата окончания периода не может быть раньше даты начала";

        return null;
    }

    private static void RecalculateStatus(KpiItem kpi)
    {
        if (kpi.Status == KpiStatus.Cancelled)
            return;

        if (kpi.CompletionPercent >= 100)
        {
            kpi.Status = KpiStatus.Completed;
            kpi.CompletedAt ??= DateTime.Now;
            return;
        }

        kpi.CompletedAt = null;

        if (DateTime.Today > kpi.PeriodEnd.Date)
        {
            kpi.Status = KpiStatus.Overdue;
            return;
        }

        kpi.Status = KpiStatus.InProgress;
    }

    private async Task<KpiOperationResult> AccessDeniedAsync(string details, string message)
    {
        await _auditLogService.LogAsync(
            AuditActionType.AccessDenied,
            false,
            "Отказано в доступе",
            details,
            user: _sessionService.CurrentUser);

        return KpiOperationResult.Fail(message);
    }
}