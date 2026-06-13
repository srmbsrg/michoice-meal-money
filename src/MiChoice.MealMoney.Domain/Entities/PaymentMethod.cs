using MiChoice.MealMoney.Domain.Enums;

namespace MiChoice.MealMoney.Domain.Entities;

public class PaymentMethod
{
    public int Id { get; set; }
    public string ParentUserId { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public CardBrand Brand { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
    public string? StripePaymentMethodId { get; set; }

    public string DisplayName => $"{Brand} •••• {Last4} ({ExpiryMonth:D2}/{ExpiryYear})";
}
