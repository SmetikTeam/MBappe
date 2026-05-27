using MBappe.Models;
using System.Collections.Generic;

namespace MBappe.Common;

public class EmployeeOperationResult
{
    public bool Success { get; }

    public string Message { get; }

    public EmployeeProfile? Employee { get; }

    public IReadOnlyList<EmployeeProfile>? Employees { get; }

    private EmployeeOperationResult(
        bool success,
        string message,
        EmployeeProfile? employee = null,
        IReadOnlyList<EmployeeProfile>? employees = null)
    {
        Success = success;
        Message = message;
        Employee = employee;
        Employees = employees;
    }

    public static EmployeeOperationResult Ok(string message)
    {
        return new EmployeeOperationResult(true, message);
    }

    public static EmployeeOperationResult Ok(EmployeeProfile employee, string message)
    {
        return new EmployeeOperationResult(true, message, employee);
    }

    public static EmployeeOperationResult Ok(IReadOnlyList<EmployeeProfile> employees, string message)
    {
        return new EmployeeOperationResult(true, message, employees: employees);
    }

    public static EmployeeOperationResult Fail(string message)
    {
        return new EmployeeOperationResult(false, message);
    }
}
