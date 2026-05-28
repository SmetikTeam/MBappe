using System;

namespace MBappe.Models;

public class LearningCourse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public LearningFormat Format { get; set; } = LearningFormat.Online;

    public string Provider { get; set; } = string.Empty;

    public double DurationHours { get; set; }

    public LearningCourseStatus Status { get; set; } = LearningCourseStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
