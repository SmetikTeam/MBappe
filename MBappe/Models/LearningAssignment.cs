using System;

namespace MBappe.Models;

public class LearningAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }

    public Guid EmployeeId { get; set; }

    public Guid AssignedByUserId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.Now;

    public DateTime? DueDate { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public double ProgressPercent { get; set; }

    public double? Score { get; set; }

    public LearningAssignmentStatus Status { get; set; } = LearningAssignmentStatus.Assigned;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
