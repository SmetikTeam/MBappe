using MBappe.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MBappe.Repositories;

public interface ILearningRepository
{
    Task<LearningCourse?> GetCourseByIdAsync(Guid id);

    Task<IReadOnlyList<LearningCourse>> GetAllCoursesAsync();

    Task AddCourseAsync(LearningCourse course);

    Task UpdateCourseAsync(LearningCourse course);

    Task<LearningAssignment?> GetAssignmentByIdAsync(Guid id);

    Task<LearningAssignment?> GetAssignmentAsync(Guid courseId, Guid employeeId);

    Task<IReadOnlyList<LearningAssignment>> GetAllAssignmentsAsync();

    Task<IReadOnlyList<LearningAssignment>> GetAssignmentsByCourseIdAsync(Guid courseId);

    Task<IReadOnlyList<LearningAssignment>> GetAssignmentsByEmployeeIdAsync(Guid employeeId);

    Task AddAssignmentAsync(LearningAssignment assignment);

    Task UpdateAssignmentAsync(LearningAssignment assignment);
}
