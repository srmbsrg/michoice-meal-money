using Microsoft.Extensions.Logging;
using MiChoice.MealMoney.Domain.Enums;

namespace MiChoice.MealMoney.Infrastructure.Services;

/// <summary>
/// Simulated payment service — logs intent and returns a synthetic success without calling Stripe.
/// This is the DEMO-install path (no real Stripe keys configured): the parent sees the full deposit
/// flow complete and the balance update, but no card is charged. Selected automatically by DI when
/// <see cref="StripeOptions.IsLive"/> is false. The live path is <see cref="StripePaymentService"/>.
/// </summary>
public class StubPaymentService : IPaymentService
{
    private readonly ILogger<StubPaymentService> _logger;

    public StubPaymentService(ILogger<StubPaymentService> logger)
    {
        _logger = logger;
    }

    public bool IsLive => false;
    public string? PublishableKey => null;

    public Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[STUB] Simulated payment of {Amount:C} for parent {ParentUserId} on account {AccountId} (no Stripe keys configured)",
            request.Amount, request.ParentUserId, request.MealMoneyAccountId);

        // Determine brand from first digit (stub heuristic)
        var brand = request.CardNumber.TrimStart().FirstOrDefault() switch
        {
            '4' => CardBrand.Visa,
            '5' => CardBrand.Mastercard,
            '3' => CardBrand.AmericanExpress,
            '6' => CardBrand.Discover,
            _   => CardBrand.Unknown
        };

        var last4 = request.CardNumber.Length >= 4 ? request.CardNumber[^4..] : "0000";

        var result = new PaymentResult(
            Success: true,
            Status: PaymentStatus.Completed,
            PaymentIntentId: $"pi_stub_{Guid.NewGuid():N}",
            Last4: last4,
            CardBrand: brand,
            ErrorMessage: null
        );

        return Task.FromResult(result);
    }

    // The Payment Element flow is never invoked in stub mode (the UI branches on IsLive).
    public Task<PaymentIntentInit> CreatePaymentIntentAsync(PaymentIntentInitRequest request, CancellationToken ct = default)
        => throw new NotSupportedException("Stub payment mode does not use PaymentIntents; configure real Stripe keys.");

    public Task<PaymentResult> ConfirmPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
        => throw new NotSupportedException("Stub payment mode does not use PaymentIntents; configure real Stripe keys.");
}
