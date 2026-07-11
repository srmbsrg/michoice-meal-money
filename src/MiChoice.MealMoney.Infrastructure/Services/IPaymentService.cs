using MiChoice.MealMoney.Domain.Enums;

namespace MiChoice.MealMoney.Infrastructure.Services;

/// <summary>Input for the simulated (stub) single-shot path. Live installs do not use this —
/// card data is tokenized client-side by the Stripe Payment Element and never reaches the server.</summary>
public record PaymentRequest(
    decimal Amount,
    string ParentUserId,
    int MealMoneyAccountId,
    string CardNumber,
    string ExpiryMonth,
    string ExpiryYear,
    string Cvc,
    string? StripePaymentMethodId = null
);

/// <summary>Request to open a real Stripe PaymentIntent for the Payment Element (live installs).</summary>
public record PaymentIntentInitRequest(decimal Amount, string ParentUserId, int MealMoneyAccountId);

/// <summary>Result of creating a PaymentIntent: the client secret + publishable key the browser's
/// Payment Element needs to collect and confirm the card.</summary>
public record PaymentIntentInit(
    bool Success,
    string? ClientSecret,
    string? PaymentIntentId,
    string? PublishableKey,
    string? ErrorMessage
);

public record PaymentResult(
    bool Success,
    PaymentStatus Status,
    string? PaymentIntentId,
    string? Last4,
    CardBrand CardBrand,
    string? ErrorMessage
);

/// <summary>
/// Payment processing abstraction. Two implementations, chosen at startup by whether real Stripe
/// keys are present (see <see cref="StripeOptions.IsLive"/>):
///   • <c>StubPaymentService</c> — simulated success, no charge. Demo installs (no keys).
///   • <c>StripePaymentService</c> — real Stripe PaymentIntents. Per-district live installs (keys set).
///
/// <see cref="IsLive"/> tells the UI which flow to render: the simulated card form (stub) or the
/// Stripe Payment Element (live). Each implementation only supports the methods for its mode; the UI
/// never calls the other set. This keeps demo installs fully offline while a district install that
/// supplies its own keys runs real charges with no code change.
/// </summary>
public interface IPaymentService
{
    /// <summary>True when real Stripe keys are configured (a live per-district install).</summary>
    bool IsLive { get; }

    /// <summary>Stripe publishable key for the browser Payment Element. Null in stub mode.</summary>
    string? PublishableKey { get; }

    /// <summary>Simulated single-shot processing. Valid only when <see cref="IsLive"/> is false.</summary>
    Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default);

    /// <summary>Create a PaymentIntent and return its client secret. Valid only when <see cref="IsLive"/> is true.</summary>
    Task<PaymentIntentInit> CreatePaymentIntentAsync(PaymentIntentInitRequest request, CancellationToken ct = default);

    /// <summary>Server-authoritative confirmation: re-fetch the PaymentIntent from Stripe and verify it
    /// genuinely succeeded before the caller credits any balance. Valid only when <see cref="IsLive"/> is true.</summary>
    Task<PaymentResult> ConfirmPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);
}
