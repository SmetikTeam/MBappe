using MBappe.Models;

namespace MBappe.Common;

public class CreateLearningCourseRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public LearningFormat Format { get; set; } = LearningFormat.Online;

    public string Provider { get; set; } = string.Empty;

    public double DurationHours { get; set; }
}
