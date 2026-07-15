namespace MiChoice.MealMoney.Infrastructure.Services;

/// <summary>
/// Database wiring for miMealMoney's OWN store (parent identity, household links,
/// payment history). Provider, location and connection all come from configuration —
/// nothing about the database is baked into the image, so relocating the file or
/// moving SQLite -> SQL Server is a config change, not a code change.
///
/// Bind path: "MiChoice:Database".
/// Environment equivalents:
///   MiChoice__Database__Provider          = Sqlite | SqlServer
///   MiChoice__Database__ConnectionString  = (provider-specific connection string)
///
/// Legacy keys ("DatabaseProvider" + "ConnectionStrings:DefaultConnection") remain
/// honoured as a fallback; the canonical section wins.
///
/// NOTE — schema evolution (TODO before this store carries anything that matters):
/// the app currently calls EnsureCreated(), which creates the schema once and then
/// NEVER alters it. That was harmless while the database was thrown away on every
/// redeploy, but now that it lives on a persistent volume an entity change will NOT
/// reach an existing database — the app will fail at runtime on the missing column.
/// Move this context to EF Core Migrations (Add-Migration + Database.Migrate(), as
/// michoice-api already does) when the monolith work reaches this service. Until then,
/// any entity change to this store requires wiping the volume's database file.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "MiChoice:Database";

    /// <summary>EF Core provider to register. "Sqlite" or "SqlServer".</summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Full provider-specific connection string. For SQLite this carries the file
    /// location (e.g. "Data Source=/data/miMealMoney.db" on a mounted volume).
    /// </summary>
    public string? ConnectionString { get; set; }
}
