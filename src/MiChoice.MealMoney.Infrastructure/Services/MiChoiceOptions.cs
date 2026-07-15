namespace MiChoice.MealMoney.Infrastructure.Services;

/// <summary>
/// How miMealMoney talks to the shared district system. Everything here is
/// configuration — the same build serves a shared-DB district install and a
/// self-contained demo, with no code change.
///
/// Bind path: "MiChoice".
/// Environment equivalents:
///   MiChoice__UseSharedAccounts = true|false
///   MiChoice__ApiBaseUrl        = https://michoice-api-production.up.railway.app
/// </summary>
public sealed class MiChoiceOptions
{
    public const string SectionName = "MiChoice";

    /// <summary>
    /// TRUE (default): student lookup and parent deposits go to the SHARED account
    /// in michoice-api — the same Account.Balance the POS reads and charges. This is
    /// the single source of truth on the money path.
    ///
    /// FALSE ("DemoSeparate"): the legacy self-contained behaviour — balances live in
    /// this app's local MealMoneyAccount table only. Kept so an isolated demo can
    /// still be run without the shared API.
    /// </summary>
    public bool UseSharedAccounts { get; set; } = true;

    /// <summary>Base address of michoice-api. Never hardcoded — supplied by config.</summary>
    public string? ApiBaseUrl { get; set; }
}
