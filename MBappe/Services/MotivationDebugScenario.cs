using MBappe.Common;
using MBappe.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Services;

public static class MotivationDebugScenario
{
    public static async Task RunAsync(
        AuthService authService,
        UserManagementService userManagementService,
        EmployeeService employeeService,
        KpiService kpiService,
        MotivationService motivationService,
        AuditLogService auditLogService)
    {
        Debug.WriteLine("=== Motivation debug scenario started ===");

        var adminLogin = await authService.LoginAsync("admin", "12345");
        Debug.WriteLine($"Admin login: {adminLogin.Success} / {adminLogin.Message}");

        var users = await userManagementService.GetAllUsersAsync();
        var developer = users.Users?.FirstOrDefault(user => user.Login == "motivation-developer");

        if (developer is null)
        {
            var createUser = await userManagementService.CreateUserAsync(new CreateUserRequest
            {
                Login = "motivation-developer",
                Email = "motivation-developer@mbappe.local",
                FullName = "Motivation Developer",
                Password = "12345",
                ConfirmPassword = "12345",
                Role = UserRole.Employee
            });

            Debug.WriteLine($"Create user: {createUser.Success} / {createUser.Message}");

            users = await userManagementService.GetAllUsersAsync();
            developer = users.Users?.FirstOrDefault(user => user.Login == "motivation-developer");
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
                PersonnelNumber = "MOT-DEV-001",
                FullName = "Motivation Developer",
                Position = "C# Developer",
                Department = "IT",
                Email = "motivation-developer@mbappe.local",
                Phone = "+7 000 000-00-02",
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

        var periodStart = DateTime.Today.AddDays(-10);
        var periodEnd = DateTime.Today.AddDays(20);

        var createTasksKpi = await kpiService.CreateKpiAsync(new CreateKpiRequest
        {
            EmployeeId = developerProfile.Id,
            Title = "Закрыть задачи для премии",
            Description = "KPI для проверки мотивационного модуля",
            TargetValue = 20,
            ActualValue = 22,
            Unit = "задач",
            WeightPercent = 60,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });

        Debug.WriteLine($"Create tasks KPI: {createTasksKpi.Success} / {createTasksKpi.Message}");

        var createQualityKpi = await kpiService.CreateKpiAsync(new CreateKpiRequest
        {
            EmployeeId = developerProfile.Id,
            Title = "Качество для премии",
            Description = "Второй KPI для расчета премии",
            TargetValue = 100,
            ActualValue = 90,
            Unit = "%",
            WeightPercent = 40,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });

        Debug.WriteLine($"Create quality KPI: {createQualityKpi.Success} / {createQualityKpi.Message}");

        var programs = await motivationService.GetProgramsAsync();
        var program = programs.Programs?.FirstOrDefault();

        if (program is null)
        {
            var createProgram = await motivationService.CreateProgramAsync(new CreateMotivationProgramRequest
            {
                Title = "Тестовая премия",
                Description = "Премия для проверки расчета",
                BaseAmount = 10_000m,
                MinEfficiencyPercent = 60,
                MaxEfficiencyPercent = 120
            });

            Debug.WriteLine($"Create program: {createProgram.Success} / {createProgram.Message}");
            program = createProgram.Program;
        }

        if (program is null)
        {
            Debug.WriteLine("Motivation program was not found. Scenario stopped.");
            return;
        }

        var calculateBonus = await motivationService.CalculateBonusAsync(new CalculateBonusRequest
        {
            EmployeeId = developerProfile.Id,
            ProgramId = program.Id,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });

        Debug.WriteLine(
            $"Calculate bonus: {calculateBonus.Success} / " +
            $"{calculateBonus.Message} / " +
            $"Amount: {calculateBonus.Bonus?.FinalAmount} / " +
            $"Efficiency: {calculateBonus.Bonus?.EfficiencyPercent}%");

        var duplicateBonus = await motivationService.CalculateBonusAsync(new CalculateBonusRequest
        {
            EmployeeId = developerProfile.Id,
            ProgramId = program.Id,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        });

        Debug.WriteLine($"Duplicate bonus blocked: {!duplicateBonus.Success} / {duplicateBonus.Message}");

        if (calculateBonus.Bonus is not null)
        {
            var approve = await motivationService.ApproveBonusAsync(calculateBonus.Bonus.Id);
            Debug.WriteLine($"Approve bonus: {approve.Success} / {approve.Message}");

            var pay = await motivationService.MarkBonusAsPaidAsync(calculateBonus.Bonus.Id);
            Debug.WriteLine($"Pay bonus: {pay.Success} / {pay.Message}");
        }

        var visibleBonuses = await motivationService.GetVisibleBonusesAsync();
        Debug.WriteLine($"Visible bonuses for admin: {visibleBonuses.Bonuses?.Count ?? 0}");

        await authService.LogoutAsync();

        var employeeLogin = await authService.LoginAsync("motivation-developer", "12345");
        Debug.WriteLine($"Employee login: {employeeLogin.Success} / {employeeLogin.Message}");

        var employeeBonuses = await motivationService.GetVisibleBonusesAsync();
        Debug.WriteLine($"Visible bonuses for employee: {employeeBonuses.Bonuses?.Count ?? 0}");

        if (calculateBonus.Bonus is not null)
        {
            var deniedPay = await motivationService.MarkBonusAsPaidAsync(calculateBonus.Bonus.Id);
            Debug.WriteLine($"Employee pay denied: {!deniedPay.Success} / {deniedPay.Message}");
        }

        var auditEntries = await auditLogService.GetAllAsync();
        Debug.WriteLine($"Audit entries: {auditEntries.Count}");

        Debug.WriteLine("=== Motivation debug scenario finished ===");
    }
}