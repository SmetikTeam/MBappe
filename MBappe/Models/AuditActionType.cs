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

    DataViewed,
    DataCreated,
    DataUpdated,
    DataDeleted,

    AccessDenied,

    SystemError
}
