using MBappe.Models;
using System.Collections.Generic;

namespace MBappe.Common;

public class LearningOperationResult
{
    public bool Success { get; }

    public string Message { get; }

    public LearningCourse? Course { get; }

    public IReadOnlyList<LearningCourse>? Courses { get; }

    public LearningAssignment? Assignment { get; }

    public IReadOnlyList<LearningAssignment>? Assignments { get; }

    private LearningOperationResult(
        bool success,
        string message,
        LearningCourse? course = null,
        IReadOnlyList<LearningCourse>? courses = null,
        LearningAssignment? assignment = null,
        IReadOnlyList<LearningAssignment>? assignments = null)
    {
        Success = success;
        Message = message;
        Course = course;
        Courses = courses;
        Assignment = assignment;
        Assignments = assignments;
    }

    public static LearningOperationResult Ok(string message)
    {
        return new LearningOperationResult(true, message);
    }

    public static LearningOperationResult Ok(LearningCourse course, string message)
    {
        return new LearningOperationResult(true, message, course);
    }

    public static LearningOperationResult Ok(IReadOnlyList<LearningCourse> courses, string message)
    {
        return new LearningOperationResult(true, message, courses: courses);
    }

    public static LearningOperationResult Ok(LearningAssignment assignment, string message)
    {
        return new LearningOperationResult(true, message, assignment: assignment);
    }

    public static LearningOperationResult Ok(IReadOnlyList<LearningAssignment> assignments, string message)
    {
        return new LearningOperationResult(true, message, assignments: assignments);
    }

    public static LearningOperationResult Fail(string message)
    {
        return new LearningOperationResult(false, message);
    }
}
