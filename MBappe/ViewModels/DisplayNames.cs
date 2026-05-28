using MBappe.Models;

namespace MBappe.ViewModels;

public static class DisplayNames
{
    public static string ForKpiStatus(KpiStatus status)
    {
        return status switch
        {
            KpiStatus.InProgress => "В работе",
            KpiStatus.Completed => "Выполнен",
            KpiStatus.Overdue => "Просрочен",
            KpiStatus.Cancelled => "Отменен",
            _ => status.ToString()
        };
    }

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

    public static string ForLearningFormat(LearningFormat format)
    {
        return format switch
        {
            LearningFormat.Online => "Онлайн",
            LearningFormat.Offline => "Очно",
            LearningFormat.Mixed => "Смешанный",
            _ => format.ToString()
        };
    }

    public static string ForLearningCourseStatus(LearningCourseStatus status)
    {
        return status switch
        {
            LearningCourseStatus.Draft => "Черновик",
            LearningCourseStatus.Active => "Активен",
            LearningCourseStatus.Archived => "В архиве",
            _ => status.ToString()
        };
    }

    public static string ForLearningAssignmentStatus(LearningAssignmentStatus status)
    {
        return status switch
        {
            LearningAssignmentStatus.Assigned => "Назначено",
            LearningAssignmentStatus.InProgress => "В процессе",
            LearningAssignmentStatus.Completed => "Завершено",
            LearningAssignmentStatus.Cancelled => "Отменено",
            _ => status.ToString()
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

            AuditActionType.KpiCreated => "Создание KPI",
            AuditActionType.KpiUpdated => "Изменение KPI",
            AuditActionType.KpiProgressUpdated => "Обновление прогресса KPI",
            AuditActionType.KpiCancelled => "Отмена KPI",
            AuditActionType.KpiViewed => "Просмотр KPI",
            AuditActionType.KpiEfficiencyCalculated => "Расчет эффективности",


            AuditActionType.LearningCourseCreated => "Создание курса",
            AuditActionType.LearningCourseUpdated => "Изменение курса",
            AuditActionType.LearningCourseViewed => "Просмотр обучения",
            AuditActionType.LearningAssigned => "Назначение обучения",
            AuditActionType.LearningProgressUpdated => "Обновление прогресса обучения",
            AuditActionType.LearningAssignmentCancelled => "Отмена обучения",

            AuditActionType.MotivationProgramCreated => "Создание программы мотивации",
            AuditActionType.MotivationProgramUpdated => "Изменение программы мотивации",
            AuditActionType.MotivationProgramViewed => "Просмотр программ мотивации",
            AuditActionType.BonusCalculated => "Расчет бонуса",
            AuditActionType.BonusApproved => "Утверждение бонуса",
            AuditActionType.BonusRejected => "Отклонение бонуса",
            AuditActionType.BonusPaid => "Выплата бонуса",
            AuditActionType.BonusCancelled => "Отмена бонуса",
            AuditActionType.BonusViewed => "Просмотр бонусов",

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

    public static string ForMotivationBonusStatus(MotivationBonusStatus status)
    {
        return status switch
        {
            MotivationBonusStatus.PendingApproval => "Ожидает утверждения",
            MotivationBonusStatus.Approved => "Утвержден",
            MotivationBonusStatus.Rejected => "Отклонен",
            MotivationBonusStatus.Paid => "Выплачен",
            MotivationBonusStatus.Cancelled => "Отменен",
            _ => status.ToString()
        };
    }
}
