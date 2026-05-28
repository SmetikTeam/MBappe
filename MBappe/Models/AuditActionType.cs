namespace MBappe.Models;

public enum AuditActionType
{
    UserLoginSuccess,
    UserLoginFailed,
    UserRegistrationSuccess,
    UserRegistrationFailed,
    UserLogout,

    UserCreated,
    UserUpdated,
    UserBlocked,
    UserUnblocked,
    UserRoleChanged,

    EmployeeCreated,
    EmployeeUpdated,
    EmployeeDismissed,
    EmployeeRestored,

    KpiCreated,
    KpiUpdated,
    KpiProgressUpdated,
    KpiCancelled,
    KpiViewed,
    KpiEfficiencyCalculated,

    LearningCourseCreated,
    LearningCourseUpdated,
    LearningCourseViewed,
    LearningAssigned,
    LearningProgressUpdated,
    LearningAssignmentCancelled,

    DataViewed,
    DataCreated,
    DataUpdated,
    DataDeleted,

    AccessDenied,

    SystemError
}
