using MBappe.Common;
using MBappe.Models;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Services;

public static class EmployeeDebugScenario
{
    public static async Task RunAsync(
        AuthService authService,
        UserManagementService userManagementService,
        EmployeeService employeeService,
        AuditLogService auditLogService)
    {
        Debug.WriteLine("=== EmployeeProfile debug scenario started ===");

        var adminLogin = await authService.LoginAsync("admin", "12345");
        Debug.WriteLine($"Admin login: {adminLogin.Success} / {adminLogin.Message}");

        var usersBeforeCreate = await userManagementService.GetAllUsersAsync();
        var developer = usersBeforeCreate.Users?.FirstOrDefault(user => user.Login == "developer");

        if (developer is null)
        {
            var createUser = await userManagementService.CreateUserAsync(new CreateUserRequest
            {
                Login = "developer",
                Email = "developer@mbappe.local",
                FullName = "Developer User",
                Password = "12345",
                ConfirmPassword = "12345",
                Role = UserRole.Employee
            });

            Debug.WriteLine($"Create AppUser developer: {createUser.Success} / {createUser.Message}");

            var usersAfterCreate = await userManagementService.GetAllUsersAsync();
            developer = usersAfterCreate.Users?.FirstOrDefault(user => user.Login == "developer");
        }

        if (developer is null)
        {
            Debug.WriteLine("Developer AppUser was not found. Scenario stopped.");
            return;
        }

        var employeesBeforeCreate = await employeeService.GetAllEmployeesAsync();
        var developerProfile = employeesBeforeCreate.Employees?.FirstOrDefault(employee => employee.UserId == developer.Id);

        if (developerProfile is null)
        {
            var createEmployee = await employeeService.CreateEmployeeAsync(new CreateEmployeeRequest
            {
                UserId = developer.Id,
                PersonnelNumber = "DEV-001",
                FullName = "Developer User",
                Position = "Junior Developer",
                Department = "IT",
                Email = "developer@mbappe.local",
                Phone = "+7 000 000-00-00"
            });

            Debug.WriteLine($"Create EmployeeProfile developer: {createEmployee.Success} / {createEmployee.Message}");
            developerProfile = createEmployee.Employee;
        }

        if (developerProfile is null)
        {
            Debug.WriteLine("Developer EmployeeProfile was not created. Scenario stopped.");
            return;
        }

        var listEmployees = await employeeService.GetAllEmployeesAsync();
        Debug.WriteLine($"Employees list: {listEmployees.Success} / count: {listEmployees.Employees?.Count ?? 0}");

        var updateEmployee = await employeeService.UpdateEmployeeAsync(new UpdateEmployeeRequest
        {
            EmployeeId = developerProfile.Id,
            FullName = developerProfile.FullName,
            Position = "Middle Developer",
            Department = developerProfile.Department,
            ManagerEmployeeId = developerProfile.ManagerEmployeeId,
            Email = developerProfile.Email,
            Phone = developerProfile.Phone
        });
        Debug.WriteLine($"Update developer position: {updateEmployee.Success} / {updateEmployee.Message}");

        var dismissEmployee = await employeeService.DismissEmployeeAsync(developerProfile.Id);
        Debug.WriteLine($"Dismiss developer: {dismissEmployee.Success} / {dismissEmployee.Employee?.Status}");

        var restoreEmployee = await employeeService.RestoreEmployeeAsync(developerProfile.Id);
        Debug.WriteLine($"Restore developer: {restoreEmployee.Success} / {restoreEmployee.Employee?.Status}");

        await authService.LogoutAsync();

        var developerLogin = await authService.LoginAsync("developer", "12345");
        Debug.WriteLine($"Developer login: {developerLogin.Success} / {developerLogin.Message}");

        var currentProfile = await employeeService.GetCurrentEmployeeProfileAsync();
        Debug.WriteLine($"Developer current profile: {currentProfile.Success} / {currentProfile.Message}");

        var deniedEmployeesList = await employeeService.GetAllEmployeesAsync();
        Debug.WriteLine($"Developer all employees denied: {!deniedEmployeesList.Success} / {deniedEmployeesList.Message}");

        var auditEntries = await auditLogService.GetAllAsync();
        Debug.WriteLine($"Audit entries: {auditEntries.Count}");
        Debug.WriteLine("=== EmployeeProfile debug scenario finished ===");
    }
}
