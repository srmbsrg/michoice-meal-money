namespace MiChoice.MealMoney.Domain.Entities;

public class HouseholdLink
{
    public int Id { get; set; }
    public string ParentUserId { get; set; } = string.Empty;
    public int MealMoneyAccountId { get; set; }
    public MealMoneyAccount Account { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public DateTime LinkedAt { get; set; }
}
