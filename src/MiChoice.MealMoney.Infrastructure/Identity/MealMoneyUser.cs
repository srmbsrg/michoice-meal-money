using Microsoft.AspNetCore.Identity;

namespace MiChoice.MealMoney.Infrastructure.Identity;

public class MealMoneyUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    /// <summary>"en" or "es"</summary>
    public string PreferredLanguage { get; set; } = "en";
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}
