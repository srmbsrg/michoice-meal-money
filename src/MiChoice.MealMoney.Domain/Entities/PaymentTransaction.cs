using MiChoice.MealMoney.Domain.Enums;

namespace MiChoice.MealMoney.Domain.Entities;

public class PaymentTransaction
{
    public int Id { get; set; }
    public int MealMoneyAccountId { get; set; }
    public MealMoneyAccount Account { get; set; } = null!;
    public string ParentUserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? Last4 { get; set; }
    public CardBrand CardBrand { get; set; }
    public DateTime InitiatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}
