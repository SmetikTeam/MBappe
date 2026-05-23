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

    DataViewed,
    DataCreated,
    DataUpdated,
    DataDeleted,

    AccessDenied,

    SystemError
}