using System;

namespace MBappe.Common;

public class UpdateKpiProgressRequest
{
    public Guid KpiId { get; set; }

    public double ActualValue { get; set; }
}