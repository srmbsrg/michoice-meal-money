namespace MiChoice.MealMoney.Domain.Entities;

public class MealMoneyAccount
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public int Grade { get; set; }
    public decimal Balance { get; set; }
    public decimal LowBalanceThreshold { get; set; } = 5.00m;
    public DateTime LastSyncedAt { get; set; }
    public ICollection<HouseholdLink> HouseholdLinks { get; set; } = new List<HouseholdLink>();
    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();

    public string FullName => $"{FirstName} {LastName}";
    public bool IsLowBalance => Balance < LowBalanceThreshold && Balance >= 0;
    public bool IsNegative => Balance < 0;
}
