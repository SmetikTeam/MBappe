using System;

namespace MBappe.Models;

public class MotivationBonus
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Guid ProgramId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public double EfficiencyPercent { get; set; }

    public decimal BaseAmount { get; set; }

    public decimal CalculatedAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public MotivationBonusStatus Status { get; set; } = MotivationBonusStatus.PendingApproval;

    public string Comment { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public Guid? RejectedByUserId { get; set; }

    public Guid? PaidByUserId { get; set; }

    public Guid? CancelledByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}