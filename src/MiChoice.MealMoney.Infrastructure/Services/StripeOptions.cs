namespace MiChoice.MealMoney.Infrastructure.Services;

/// <summary>
/// Stripe configuration, bound from the "Stripe" config section. Under the per-district delivery
/// model each install supplies its own keys via config/secrets (e.g. Railway/host env vars); our
/// demo installs ship placeholder keys, so <see cref="IsLive"/> is false and the app falls back to
/// the simulated stub flow (no real charges).
/// </summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string? PublishableKey { get; set; }
    public string? SecretKey { get; set; }

    /// <summary>ISO currency for PaymentIntents. School meal accounts are USD.</summary>
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// True only when BOTH a real publishable and secret key are present. Placeholder values shipped
    /// in appsettings ("sk_test_REPLACE_ME", "pk_live_...", empty, or anything containing "REPLACE")
    /// are treated as absent so a demo install never hands Stripe a fake key — it stays on the stub.
    /// A real per-district install sets both keys and flips this to true.
    /// </summary>
    public bool IsLive => IsRealKey(SecretKey, "sk_") && IsRealKey(PublishableKey, "pk_");

    private static bool IsRealKey(string? key, string prefix) =>
        !string.IsNullOrWhiteSpace(key) &&
        key.StartsWith(prefix, StringComparison.Ordinal) &&
        key.Length > 20 &&
        !key.Contains("...", StringComparison.Ordinal) &&
        !key.Contains("REPLACE", StringComparison.OrdinalIgnoreCase);
}
