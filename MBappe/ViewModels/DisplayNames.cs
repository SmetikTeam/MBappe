using MBappe.Models;

namespace MBappe.ViewModels;

public static class DisplayNames
{
    public static string ForRole(UserRole role)
    {
        return role switch
        {
            UserRole.Employee => "Сотрудник",
            UserRole.Manager => "Руководитель",
            UserRole.HrSpecialist => "HR-специалист",
            UserRole.Administrator => "Администратор",
            _ => role.ToString()
        };
    }

    public static string ForAuditAction(AuditActionType actionType)
    {
        return actionType switch
        {
            AuditActionType.UserLoginSuccess => "Вход выполнен",
            AuditActionType.UserLoginFailed => "Ошибка входа",
            AuditActionType.UserRegistrationSuccess => "Регистрация",
            AuditActionType.UserRegistrationFailed => "Ошибка регистрации",
            AuditActionType.UserLogout => "Выход",
            AuditActionType.UserCreated => "Создание пользователя",
            AuditActionType.UserUpdated => "Изменение пользователя",
            AuditActionType.UserBlocked => "Блокировка пользователя",
            AuditActionType.UserUnblocked => "Разблокировка пользователя",
            AuditActionType.UserRoleChanged => "Изменение роли",
            AuditActionType.EmployeeCreated => "Создание сотрудника",
            AuditActionType.EmployeeUpdated => "Изменение сотрудника",
            AuditActionType.EmployeeDismissed => "Увольнение сотрудника",
            AuditActionType.EmployeeRestored => "Восстановление сотрудника",
            AuditActionType.DataViewed => "Просмотр данных",
            AuditActionType.DataCreated => "Создание данных",
            AuditActionType.DataUpdated => "Изменение данных",
            AuditActionType.DataDeleted => "Удаление данных",
            AuditActionType.AccessDenied => "Отказ в доступе",
            AuditActionType.SystemError => "Системная ошибка",
            _ => actionType.ToString()
        };
    }

    public static string ForEmployeeStatus(EmployeeStatus status)
    {
        return status switch
        {
            EmployeeStatus.Active => "Активен",
            EmployeeStatus.OnVacation => "В отпуске",
            EmployeeStatus.SickLeave => "На больничном",
            EmployeeStatus.Dismissed => "Уволен",
            _ => status.ToString()
        };
    }
}
