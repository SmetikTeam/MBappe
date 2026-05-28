using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public interface IMotivationBonusRepository
{
    Task<MotivationBonus?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<MotivationBonus>> GetAllAsync();

    Task<IReadOnlyList<MotivationBonus>> GetByEmployeeIdAsync(Guid employeeId);

    Task<MotivationBonus?> FindExistingAsync(
        Guid employeeId,
        Guid programId,
        DateTime periodStart,
        DateTime periodEnd);

    Task AddAsync(MotivationBonus bonus);

    Task UpdateAsync(MotivationBonus bonus);
}