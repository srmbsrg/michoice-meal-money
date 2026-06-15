using MiChoice.MealMoney.Domain.Enums;

namespace MiChoice.MealMoney.Domain.Entities;

/// <summary>
/// District admin refund request for a completed parent payment.
/// One transaction may have at most one active refund request.
/// Stripe refund ID populated when refund is processed.
/// </summary>
public class RefundRequest
{
    public int Id { get; set; }

    /// <summary>FK to the PaymentTransaction being refunded.</summary>
    public int PaymentTransactionId { get; set; }
    public PaymentTransaction Transaction { get; set; } = null!;

    /// <summary>Refund amount — may be partial (≤ Transaction.Amount).</summary>
    public decimal Amount { get; set; }

    /// <summary>District admin's reason for the refund.</summary>
    public string Reason { get; set; } = string.Empty;

    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    /// <summary>Identity user ID of the district admin who created the request.</summary>
    public string RequestedBy { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Admin who approved or denied.</summary>
    public string? ReviewedBy { get; set; }

    /// <summary>Stripe refund ID — populated after Stripe processes the refund.</summary>
    public string? StripeRefundId { get; set; }

    public DateTime? ProcessedAt { get; set; }
    public string? DenialReason { get; set; }
    public string? Notes { get; set; }
}
