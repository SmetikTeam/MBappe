using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public class InMemoryLearningRepository : ILearningRepository
{
    private readonly List<LearningCourse> _courses = [];

    private readonly List<LearningAssignment> _assignments = [];

    public Task<LearningCourse?> GetCourseByIdAsync(Guid id)
    {
        var course = _courses.FirstOrDefault(course => course.Id == id);
        return Task.FromResult(course);
    }

    public Task<IReadOnlyList<LearningCourse>> GetAllCoursesAsync()
    {
        var courses = _courses
            .OrderBy(course => course.Title)
            .ToList();

        return Task.FromResult<IReadOnlyList<LearningCourse>>(courses);
    }

    public Task AddCourseAsync(LearningCourse course)
    {
        _courses.Add(course);
        return Task.CompletedTask;
    }

    public Task UpdateCourseAsync(LearningCourse course)
    {
        return Task.CompletedTask;
    }

    public Task<LearningAssignment?> GetAssignmentByIdAsync(Guid id)
    {
        var assignment = _assignments.FirstOrDefault(assignment => assignment.Id == id);
        return Task.FromResult(assignment);
    }

    public Task<LearningAssignment?> GetAssignmentAsync(Guid courseId, Guid employeeId)
    {
        var assignment = _assignments.FirstOrDefault(assignment =>
            assignment.CourseId == courseId && assignment.EmployeeId == employeeId);

        return Task.FromResult(assignment);
    }

    public Task<IReadOnlyList<LearningAssignment>> GetAllAssignmentsAsync()
    {
        var assignments = _assignments
            .OrderByDescending(assignment => assignment.AssignedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<LearningAssignment>>(assignments);
    }

    public Task<IReadOnlyList<LearningAssignment>> GetAssignmentsByCourseIdAsync(Guid courseId)
    {
        var assignments = _assignments
            .Where(assignment => assignment.CourseId == courseId)
            .OrderByDescending(assignment => assignment.AssignedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<LearningAssignment>>(assignments);
    }

    public Task<IReadOnlyList<LearningAssignment>> GetAssignmentsByEmployeeIdAsync(Guid employeeId)
    {
        var assignments = _assignments
            .Where(assignment => assignment.EmployeeId == employeeId)
            .OrderByDescending(assignment => assignment.AssignedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<LearningAssignment>>(assignments);
    }

    public Task AddAssignmentAsync(LearningAssignment assignment)
    {
        _assignments.Add(assignment);
        return Task.CompletedTask;
    }

    public Task UpdateAssignmentAsync(LearningAssignment assignment)
    {
        return Task.CompletedTask;
    }
}
