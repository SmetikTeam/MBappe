namespace MBappe.Common;

public class CreateMotivationProgramRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal BaseAmount { get; set; }

    public double MinEfficiencyPercent { get; set; } = 60;

    public double MaxEfficiencyPercent { get; set; } = 120;
}