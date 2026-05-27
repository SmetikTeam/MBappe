using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public interface IEmployeeRepository
{
    Task<EmployeeProfile?> GetByIdAsync(Guid id);

    Task<EmployeeProfile?> GetByUserIdAsync(Guid userId);

    Task<EmployeeProfile?> GetByPersonnelNumberAsync(string personnelNumber);

    Task<IReadOnlyList<EmployeeProfile>> GetAllAsync();

    Task AddAsync(EmployeeProfile employee);

    Task UpdateAsync(EmployeeProfile employee);
}
