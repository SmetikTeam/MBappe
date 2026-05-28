using MBappe.Models;
using System;

namespace MBappe.Common;

public class UpdateLearningCourseRequest
{
    public Guid CourseId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public LearningFormat Format { get; set; } = LearningFormat.Online;

    public string Provider { get; set; } = string.Empty;

    public double DurationHours { get; set; }

    public LearningCourseStatus Status { get; set; } = LearningCourseStatus.Draft;
}
