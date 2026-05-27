using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly List<EmployeeProfile> _employees = [];

    public Task<EmployeeProfile?> GetByIdAsync(Guid id)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(employee);
    }

    public Task<EmployeeProfile?> GetByUserIdAsync(Guid userId)
    {
        var employee = _employees.FirstOrDefault(e => e.UserId == userId);
        return Task.FromResult(employee);
    }

    public Task<EmployeeProfile?> GetByPersonnelNumberAsync(string personnelNumber)
    {
        var employee = _employees.FirstOrDefault(e =>
            string.Equals(e.PersonnelNumber, personnelNumber, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(employee);
    }

    public Task<IReadOnlyList<EmployeeProfile>> GetAllAsync()
    {
        var employees = _employees
            .OrderBy(e => e.FullName)
            .ToList();

        return Task.FromResult<IReadOnlyList<EmployeeProfile>>(employees);
    }

    public Task AddAsync(EmployeeProfile employee)
    {
        _employees.Add(employee);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EmployeeProfile employee)
    {
        return Task.CompletedTask;
    }
}
