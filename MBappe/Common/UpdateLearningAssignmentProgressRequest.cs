using System;

namespace MBappe.Common;

public class UpdateLearningAssignmentProgressRequest
{
    public Guid AssignmentId { get; set; }

    public double ProgressPercent { get; set; }

    public double? Score { get; set; }
}
