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
