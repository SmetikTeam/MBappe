using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace MBappe.Repositories;

public class InMemoryMotivationProgramRepository : IMotivationProgramRepository
{
    private readonly List<MotivationProgram> _programs = [];

    public InMemoryMotivationProgramRepository()
    {
        SeedPrograms();
    }

    public Task<MotivationProgram?> GetByIdAsync(Guid id)
    {
        var program = _programs.FirstOrDefault(program => program.Id == id);

        return Task.FromResult(program);
    }

    public Task<IReadOnlyList<MotivationProgram>> GetAllAsync()
    {
        var programs = _programs
            .OrderByDescending(program => program.IsActive)
            .ThenBy(program => program.Title)
            .ToList();

        return Task.FromResult<IReadOnlyList<MotivationProgram>>(programs);
    }

    public Task AddAsync(MotivationProgram program)
    {
        _programs.Add(program);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(MotivationProgram program)
    {
        return Task.CompletedTask;
    }

    private void SeedPrograms()
    {
        _programs.Add(new MotivationProgram
        {
            Title = "Ежемесячная премия по KPI",
            Description = "Базовая программа премирования сотрудников по результатам выполнения KPI.",
            BaseAmount = 10_000m,
            MinEfficiencyPercent = 60,
            MaxEfficiencyPercent = 120,
            IsActive = true,
            CreatedByUserId = Guid.Empty,
            CreatedAt = DateTime.Now
        });
    }
}