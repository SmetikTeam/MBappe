using MBappe.Models;
using MBappe.Services;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace MBappe.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<AppUser> _users = [];

    public InMemoryUserRepository()
    {
        SeedUsers();
    }

    public Task<AppUser?> GetByLoginAsync(string login)
    {
        var user = _users.FirstOrDefault(u =>
            string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }

    public Task<AppUser?> GetByEmailAsync(string email)
    {
        var user = _users.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(user);
    }

    public Task AddAsync(AppUser user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AppUser user)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AppUser>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<AppUser>>(_users);
    }

    private void SeedUsers()
    {
        AddSeedUser("employee", "employee@mbappe.local", "Иван Петров", "12345", UserRole.Employee);
        AddSeedUser("manager", "manager@mbappe.local", "Анна Смирнова", "12345", UserRole.Manager);
        AddSeedUser("hr", "hr@mbappe.local", "Мария HR", "12345", UserRole.HrSpecialist);
        AddSeedUser("admin", "admin@mbappe.local", "Администратор", "12345", UserRole.Administrator);
    }

    private void AddSeedUser(string login, string email, string fullName, string password, UserRole role)
    {
        var hasher = new PasswordHasher();
        var salt = hasher.GenerateSalt();
        var hash = hasher.HashPassword(password, salt);

        _users.Add(new AppUser
        {
            Login = login,
            Email = email,
            FullName = fullName,
            PasswordSalt = salt,
            PasswordHash = hash,
            Role = role,
            IsActive = true
        });
    }
}