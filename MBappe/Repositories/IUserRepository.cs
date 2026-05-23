using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id);

    Task<AppUser?> GetByLoginAsync(string login);

    Task<AppUser?> GetByEmailAsync(string email);

    Task AddAsync(AppUser user);

    Task UpdateAsync(AppUser user);

    Task<IReadOnlyList<AppUser>> GetAllAsync();
}