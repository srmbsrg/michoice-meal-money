using Microsoft.Extensions.Logging;
using MiChoice.MealMoney.Domain.Enums;

namespace MiChoice.MealMoney.Infrastructure.Services;

/// <summary>
/// Stub implementation — logs intent but does not call Stripe.
/// TODO: Replace with real Stripe PaymentIntents API call.
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(ILogger<StripePaymentService> logger)
    {
        _logger = logger;
    }

    public Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[STUB] Processing payment of {Amount:C} for parent {ParentUserId} on account {AccountId}",
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

        var last4 = request.CardNumber.Length >= 4
            ? request.CardNumber[^4..]
            : "0000";

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
}
