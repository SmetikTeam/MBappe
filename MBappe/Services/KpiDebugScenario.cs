using MBappe.Common;
using MBappe.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Services;

public static class KpiDebugScenario
{
    public static async Task RunAsync(
        AuthService authService,
        UserManagementService userManagementService,
        EmployeeService employeeService,
        KpiService kpiService,
        AuditLogService auditLogService)
    {
        Debug.WriteLine("=== Kpi debug scenario started ===");

        var adminLogin = await authService.LoginAsync("admin", "12345");
        Debug.WriteLine($"Admin login: {adminLogin.Success} / {adminLogin.Message}");

        var users = await userManagementService.GetAllUsersAsync();
        var developer = users.Users?.FirstOrDefault(user => user.Login == "kpi-developer");

        if (developer is null)
        {
            var createUser = await userManagementService.CreateUserAsync(new CreateUserRequest
            {
                Login = "kpi-developer",
                Email = "kpi-developer@mbappe.local",
                FullName = "KPI Developer",
                Password = "12345",
                ConfirmPassword = "12345",
                Role = UserRole.Employee
            });

            Debug.WriteLine($"Create user: {createUser.Success} / {createUser.Message}");

            users = await userManagementService.GetAllUsersAsync();
            developer = users.Users?.FirstOrDefault(user => user.Login == "kpi-developer");
        }

        if (developer is null)
        {
            Debug.WriteLine("Developer user was not found. Scenario stopped.");
            return;
        }

        var employees = await employeeService.GetAllEmployeesAsync();
        var developerProfile = employees.Employees?.FirstOrDefault(employee => employee.UserId == developer.Id);

        if (developerProfile is null)
        {
            var createEmployee = await employeeService.CreateEmployeeAsync(new CreateEmployeeRequest
            {
                UserId = developer.Id,
                PersonnelNumber = "KPI-DEV-001",
                FullName = "KPI Developer",
                Position = "C# Developer",
                Department = "IT",
                Email = "kpi-developer@mbappe.local",
                Phone = "+7 000 000-00-01",
                HireDate = DateTime.Today
            });

            Debug.WriteLine($"Create employee: {createEmployee.Success} / {createEmployee.Message}");
            developerProfile = createEmployee.Employee;
        }

        if (developerProfile is null)
        {
            Debug.WriteLine("Developer employee profile was not found. Scenario stopped.");
            return;
        }

        var createTasksKpi = await kpiService.CreateKpiAsync(new CreateKpiRequest
        {
            EmployeeId = developerProfile.Id,
            Title = "Закрыть задачи",
            Description = "Закрыть задачи в рамках текущего спринта",
            TargetValue = 20,
            ActualValue = 15,
            Unit = "задач",
            WeightPercent = 60,
            PeriodStart = DateTime.Today.AddDays(-10),
            PeriodEnd = DateTime.Today.AddDays(20)
        });

        Debug.WriteLine($"Create tasks KPI: {createTasksKpi.Success} / {createTasksKpi.Message}");

        var createQualityKpi = await kpiService.CreateKpiAsync(new CreateKpiRequest
        {
            EmployeeId = developerProfile.Id,
            Title = "Качество выполнения",
            Description = "Доля задач без возврата на доработку",
            TargetValue = 100,
            ActualValue = 90,
            Unit = "%",
            WeightPercent = 40,
            PeriodStart = DateTime.Today.AddDays(-10),
            PeriodEnd = DateTime.Today.AddDays(20)
        });

        Debug.WriteLine($"Create quality KPI: {createQualityKpi.Success} / {createQualityKpi.Message}");

        if (createTasksKpi.Kpi is not null)
        {
            var updateProgress = await kpiService.UpdateKpiProgressAsync(new UpdateKpiProgressRequest
            {
                KpiId = createTasksKpi.Kpi.Id,
                ActualValue = 22
            });

            Debug.WriteLine(
                $"Update KPI progress: {updateProgress.Success} / " +
                $"{updateProgress.Kpi?.CompletionPercent}% / {updateProgress.Kpi?.Status}");
        }

        var employeeKpis = await kpiService.GetEmployeeKpisAsync(developerProfile.Id);
        Debug.WriteLine($"Employee KPI count: {employeeKpis.Kpis?.Count ?? 0}");

        var efficiency = await kpiService.GetEmployeeEfficiencyAsync(
            developerProfile.Id,
            DateTime.Today.AddMonths(-1),
            DateTime.Today.AddMonths(1));

        Debug.WriteLine(
            $"Efficiency: {efficiency.Success} / " +
            $"{efficiency.Efficiency?.EfficiencyPercent}% / " +
            $"KPI count: {efficiency.Efficiency?.KpiCount}");

        await authService.LogoutAsync();

        var employeeLogin = await authService.LoginAsync("kpi-developer", "12345");
        Debug.WriteLine($"Employee login: {employeeLogin.Success} / {employeeLogin.Message}");

        var visibleKpis = await kpiService.GetVisibleKpisAsync();
        Debug.WriteLine($"Employee visible KPI count: {visibleKpis.Kpis?.Count ?? 0}");

        var deniedCreate = await kpiService.CreateKpiAsync(new CreateKpiRequest
        {
            EmployeeId = developerProfile.Id,
            Title = "Запрещенный KPI",
            TargetValue = 10,
            ActualValue = 0,
            Unit = "шт",
            WeightPercent = 100,
            PeriodStart = DateTime.Today,
            PeriodEnd = DateTime.Today.AddDays(30)
        });

        Debug.WriteLine($"Employee create KPI denied: {!deniedCreate.Success} / {deniedCreate.Message}");

        var auditEntries = await auditLogService.GetAllAsync();
        Debug.WriteLine($"Audit entries: {auditEntries.Count}");

        Debug.WriteLine("=== Kpi debug scenario finished ===");
    }
}