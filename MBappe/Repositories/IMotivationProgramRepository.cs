using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public interface IMotivationProgramRepository
{
    Task<MotivationProgram?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<MotivationProgram>> GetAllAsync();

    Task AddAsync(MotivationProgram program);

    Task UpdateAsync(MotivationProgram program);
}