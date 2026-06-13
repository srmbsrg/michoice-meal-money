using MiChoice.MealMoney.Domain.Enums;

namespace MiChoice.MealMoney.Infrastructure.Services;

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

public record PaymentResult(
    bool Success,
    PaymentStatus Status,
    string? PaymentIntentId,
    string? Last4,
    CardBrand CardBrand,
    string? ErrorMessage
);

public interface IPaymentService
{
    Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default);
}
