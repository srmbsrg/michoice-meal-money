namespace MiChoice.MealMoney.Domain.Enums;

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}

public enum CardBrand
{
    Unknown = 0,
    Visa = 1,
    Mastercard = 2,
    AmericanExpress = 3,
    Discover = 4
}

/// <summary>
/// Lifecycle status for a district admin refund request.
/// </summary>
public enum RefundStatus
{
    /// <summary>Request submitted, awaiting admin review.</summary>
    Pending = 0,

    /// <summary>Request approved — queued for Stripe processing.</summary>
    Approved = 1,

    /// <summary>Refund issued via Stripe. StripeRefundId is populated.</summary>
    Processed = 2,

    /// <summary>Request denied. DenialReason is populated.</summary>
    Denied = 3
}
