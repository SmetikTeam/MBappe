using MBappe.Data;
using MBappe.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public class EfLearningRepository : ILearningRepository
{
    private readonly Func<AppDbContext> _dbFactory;

    public EfLearningRepository(Func<AppDbContext>? dbFactory = null)
    {
        _dbFactory = dbFactory ?? (() => new AppDbContext());
    }
    
    public async Task<LearningCourse?> GetCourseByIdAsync(Guid id)
    {
        await using var db = _dbFactory();

        return await db.LearningCourses
            .AsNoTracking()
            .FirstOrDefaultAsync(course => course.Id == id);
    }

    public async Task<IReadOnlyList<LearningCourse>> GetAllCoursesAsync()
    {
        await using var db = _dbFactory();

        return await db.LearningCourses
            .AsNoTracking()
            .OrderByDescending(course => course.Status == LearningCourseStatus.Active)
            .ThenBy(course => course.Title)
            .ToListAsync();
    }

    public async Task AddCourseAsync(LearningCourse course)
    {
        await using var db = _dbFactory();

        db.LearningCourses.Add(course);

        await db.SaveChangesAsync();
    }

    public async Task UpdateCourseAsync(LearningCourse course)
    {
        await using var db = _dbFactory();

        db.LearningCourses.Update(course);

        await db.SaveChangesAsync();
    }

    public async Task<LearningAssignment?> GetAssignmentByIdAsync(Guid id)
    {
        await using var db = _dbFactory();

        return await db.LearningAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(assignment => assignment.Id == id);
    }

    public async Task<LearningAssignment?> GetAssignmentAsync(Guid courseId, Guid employeeId)
    {
        await using var db = _dbFactory();

        return await db.LearningAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(assignment =>
                assignment.CourseId == courseId
                && assignment.EmployeeId == employeeId
                && assignment.Status != LearningAssignmentStatus.Cancelled);
    }

    public async Task<IReadOnlyList<LearningAssignment>> GetAllAssignmentsAsync()
    {
        await using var db = _dbFactory();

        return await db.LearningAssignments
            .AsNoTracking()
            .OrderByDescending(assignment => assignment.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LearningAssignment>> GetAssignmentsByCourseIdAsync(Guid courseId)
    {
        await using var db = _dbFactory();

        return await db.LearningAssignments
            .AsNoTracking()
            .Where(assignment => assignment.CourseId == courseId)
            .OrderByDescending(assignment => assignment.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LearningAssignment>> GetAssignmentsByEmployeeIdAsync(Guid employeeId)
    {
        await using var db = _dbFactory();

        return await db.LearningAssignments
            .AsNoTracking()
            .Where(assignment => assignment.EmployeeId == employeeId)
            .OrderByDescending(assignment => assignment.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAssignmentAsync(LearningAssignment assignment)
    {
        await using var db = _dbFactory();

        db.LearningAssignments.Add(assignment);

        await db.SaveChangesAsync();
    }

    public async Task UpdateAssignmentAsync(LearningAssignment assignment)
    {
        await using var db = _dbFactory();

        db.LearningAssignments.Update(assignment);

        await db.SaveChangesAsync();
    }
}