namespace MBappe.Models;

public enum AuditActionType
{
    UserLoginSuccess,
    UserLoginFailed,
    UserRegistered,
    UserLogout,

    UserCreated,
    UserUpdated,
    UserDeleted,

    RoleChanged,

    DataViewed,
    DataCreated,
    DataUpdated,
    DataDeleted,

    SystemError
}