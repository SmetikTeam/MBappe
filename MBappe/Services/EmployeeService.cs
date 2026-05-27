using MBappe.Common;
using MBappe.Models;
using MBappe.Repositories;
using System;
using System.Threading.Tasks;

namespace MBappe.Services;

public class EmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly SessionService _sessionService;
    private readonly AuditLogService _auditLogService;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        SessionService sessionService,
        AuditLogService auditLogService)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _sessionService = sessionService;
        _auditLogService = auditLogService;
    }

    public bool CanManageEmployees()
    {
        return _sessionService.HasAnyRole(
            UserRole.Administrator,
            UserRole.HrSpecialist);
    }

    public bool CanViewAllEmployees()
    {
        return _sessionService.HasAnyRole(
            UserRole.Administrator,
            UserRole.HrSpecialist,
            UserRole.Manager);
    }

    public async Task<EmployeeOperationResult> GetAllEmployeesAsync()
    {
        if (!CanViewAllEmployees())
            return await AccessDeniedAsync("Попытка получить список сотрудников", "Недостаточно прав для просмотра списка сотрудников");

        var employees = await _employeeRepository.GetAllAsync();

        await _auditLogService.LogAsync(
            AuditActionType.DataViewed,
            true,
            "Получен список сотрудников",
            $"Количество сотрудников: {employees.Count}");

        return EmployeeOperationResult.Ok(employees, "Список сотрудников получен");
    }

    public async Task<EmployeeOperationResult> GetEmployeeByIdAsync(Guid employeeId)
    {
        if (employeeId == Guid.Empty)
            return EmployeeOperationResult.Fail("Не указан сотрудник");

        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee is null)
            return EmployeeOperationResult.Fail("Профиль сотрудника не найден");

        if (!CanViewAllEmployees() && employee.UserId != _sessionService.CurrentUser?.Id)
            return await AccessDeniedAsync("Попытка просмотреть чужой профиль сотрудника", "Недостаточно прав для просмотра профиля сотрудника");

        await _auditLogService.LogAsync(
            AuditActionType.DataViewed,
            true,
            "Получен профиль сотрудника",
            $"Табельный номер: {employee.PersonnelNumber}");

        return EmployeeOperationResult.Ok(employee, "Профиль сотрудника получен");
    }

    public async Task<EmployeeOperationResult> GetCurrentEmployeeProfileAsync()
    {
        var currentUser = _sessionService.CurrentUser;

        if (currentUser is null)
            return await AccessDeniedAsync("Попытка получить профиль без активной сессии", "Пользователь не авторизован");

        var employee = await _employeeRepository.GetByUserIdAsync(currentUser.Id);

        if (employee is null)
            return EmployeeOperationResult.Fail("Для текущей учетной записи еще не создан профиль сотрудника");

        await _auditLogService.LogAsync(
            AuditActionType.DataViewed,
            true,
            "Получен текущий профиль сотрудника",
            $"Пользователь: {currentUser.Login}",
            user: currentUser);

        return EmployeeOperationResult.Ok(employee, "Профиль сотрудника получен");
    }

    public async Task<EmployeeOperationResult> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        if (!CanManageEmployees())
            return await AccessDeniedAsync("Попытка создать профиль сотрудника", "Недостаточно прав для создания сотрудника");

        var validationError = await ValidateCreateRequestAsync(request);

        if (validationError is not null)
            return EmployeeOperationResult.Fail(validationError);

        var employee = new EmployeeProfile
        {
            UserId = request.UserId,
            PersonnelNumber = request.PersonnelNumber.Trim(),
            FullName = request.FullName.Trim(),
            Position = request.Position.Trim(),
            Department = request.Department.Trim(),
            ManagerEmployeeId = request.ManagerEmployeeId,
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            HireDate = request.HireDate.Date,
            Status = EmployeeStatus.Active,
            CreatedAt = DateTime.Now
        };

        await _employeeRepository.AddAsync(employee);

        await _auditLogService.LogAsync(
            AuditActionType.EmployeeCreated,
            true,
            "Создан профиль сотрудника",
            $"Табельный номер: {employee.PersonnelNumber}, пользователь: {employee.UserId}");

        return EmployeeOperationResult.Ok(employee, "Профиль сотрудника создан");
    }

    public async Task<EmployeeOperationResult> UpdateEmployeeAsync(UpdateEmployeeRequest request)
    {
        if (!CanManageEmployees())
            return await AccessDeniedAsync("Попытка изменить профиль сотрудника", "Недостаточно прав для изменения сотрудника");

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);

        if (employee is null)
            return EmployeeOperationResult.Fail("Профиль сотрудника не найден");

        var validationError = await ValidateUpdateRequestAsync(request, employee);

        if (validationError is not null)
            return EmployeeOperationResult.Fail(validationError);

        employee.FullName = request.FullName.Trim();
        employee.Position = request.Position.Trim();
        employee.Department = request.Department.Trim();
        employee.ManagerEmployeeId = request.ManagerEmployeeId;
        employee.Email = request.Email.Trim();
        employee.Phone = request.Phone.Trim();
        employee.UpdatedAt = DateTime.Now;

        await _employeeRepository.UpdateAsync(employee);

        await _auditLogService.LogAsync(
            AuditActionType.EmployeeUpdated,
            true,
            "Изменен профиль сотрудника",
            $"Табельный номер: {employee.PersonnelNumber}");

        return EmployeeOperationResult.Ok(employee, "Профиль сотрудника обновлен");
    }

    public async Task<EmployeeOperationResult> DismissEmployeeAsync(Guid employeeId)
    {
        if (!CanManageEmployees())
            return await AccessDeniedAsync("Попытка уволить сотрудника", "Недостаточно прав для увольнения сотрудника");

        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee is null)
            return EmployeeOperationResult.Fail("Профиль сотрудника не найден");

        if (employee.Status == EmployeeStatus.Dismissed)
            return EmployeeOperationResult.Fail("Сотрудник уже уволен");

        employee.Status = EmployeeStatus.Dismissed;
        employee.DismissalDate = DateTime.Today;
        employee.UpdatedAt = DateTime.Now;

        await _employeeRepository.UpdateAsync(employee);

        await _auditLogService.LogAsync(
            AuditActionType.EmployeeDismissed,
            true,
            "Сотрудник уволен",
            $"Табельный номер: {employee.PersonnelNumber}");

        return EmployeeOperationResult.Ok(employee, "Сотрудник уволен");
    }

    public async Task<EmployeeOperationResult> RestoreEmployeeAsync(Guid employeeId)
    {
        if (!CanManageEmployees())
            return await AccessDeniedAsync("Попытка восстановить сотрудника", "Недостаточно прав для восстановления сотрудника");

        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee is null)
            return EmployeeOperationResult.Fail("Профиль сотрудника не найден");

        if (employee.Status != EmployeeStatus.Dismissed)
            return EmployeeOperationResult.Fail("Сотрудник не уволен");

        employee.Status = EmployeeStatus.Active;
        employee.DismissalDate = null;
        employee.UpdatedAt = DateTime.Now;

        await _employeeRepository.UpdateAsync(employee);

        await _auditLogService.LogAsync(
            AuditActionType.EmployeeRestored,
            true,
            "Сотрудник восстановлен",
            $"Табельный номер: {employee.PersonnelNumber}");

        return EmployeeOperationResult.Ok(employee, "Сотрудник восстановлен");
    }

    private async Task<string?> ValidateCreateRequestAsync(CreateEmployeeRequest request)
    {
        if (request.UserId == Guid.Empty)
            return "Выберите учетную запись пользователя";

        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user is null)
            return "Учетная запись пользователя не найдена";

        var existingEmployeeForUser = await _employeeRepository.GetByUserIdAsync(request.UserId);

        if (existingEmployeeForUser is not null)
            return "Для этой учетной записи уже создан профиль сотрудника";

        if (string.IsNullOrWhiteSpace(request.PersonnelNumber))
            return "Введите табельный номер";

        var employeeWithSameNumber = await _employeeRepository.GetByPersonnelNumberAsync(request.PersonnelNumber.Trim());

        if (employeeWithSameNumber is not null)
            return "Сотрудник с таким табельным номером уже существует";

        return await ValidateEmployeeFieldsAsync(
            request.FullName,
            request.Position,
            request.Department,
            request.ManagerEmployeeId,
            request.Email);
    }

    private async Task<string?> ValidateUpdateRequestAsync(UpdateEmployeeRequest request, EmployeeProfile employee)
    {
        if (request.EmployeeId == Guid.Empty)
            return "Не указан сотрудник";

        if (request.ManagerEmployeeId == employee.Id)
            return "Сотрудник не может быть собственным руководителем";

        return await ValidateEmployeeFieldsAsync(
            request.FullName,
            request.Position,
            request.Department,
            request.ManagerEmployeeId,
            request.Email);
    }

    private async Task<string?> ValidateEmployeeFieldsAsync(
        string fullName,
        string position,
        string department,
        Guid? managerEmployeeId,
        string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "Введите ФИО";

        if (string.IsNullOrWhiteSpace(position))
            return "Введите должность";

        if (string.IsNullOrWhiteSpace(department))
            return "Введите отдел";

        if (string.IsNullOrWhiteSpace(email))
            return "Введите почту";

        if (!email.Contains('@'))
            return "Некорректная почта";

        if (managerEmployeeId is not null)
        {
            var manager = await _employeeRepository.GetByIdAsync(managerEmployeeId.Value);

            if (manager is null)
                return "Указанный руководитель не найден";
        }

        return null;
    }

    private async Task<EmployeeOperationResult> AccessDeniedAsync(string details, string message)
    {
        await _auditLogService.LogAsync(
            AuditActionType.AccessDenied,
            false,
            "Отказано в доступе",
            details,
            user: _sessionService.CurrentUser);

        return EmployeeOperationResult.Fail(message);
    }
}
