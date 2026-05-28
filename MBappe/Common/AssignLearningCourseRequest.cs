using System;

namespace MBappe.Common;

public class AssignLearningCourseRequest
{
    public Guid CourseId { get; set; }

    public Guid EmployeeId { get; set; }

    public DateTime? DueDate { get; set; }
}
