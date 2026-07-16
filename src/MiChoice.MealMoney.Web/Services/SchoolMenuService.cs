namespace MiChoice.MealMoney.Web.Services;

// ─────────────────────────────────────────────────────────────────────────────
// SchoolMenuService — the PARENT-FACING menu source.
//
// FABRICATED MENU ROTATIONS REMOVED (2026-07-16).
//
// What this file used to be: ~190 lines of invented food. Four hand-written
// rotations (5 breakfasts, 5 lunches, 3 snacks, 3 after-school suppers) — "Cheese
// Pizza Day", "Taco Tuesday", "Whole-Grain Cheese Pizza 350 cal, allergens Wheat/
// Milk" — indexed by (date.DayOfYear + campusOffset) % rotation.Length so that
// EVERY school day of EVERY month, past or future, for every campus, rendered a
// full menu with dish names, calorie counts, allergen lists and a-la-carte prices.
//
// None of it came from anywhere. There is no menu feed into this app: no DB, no
// gateway call, no michoice-api client. A parent reading this calendar was reading
// a rotation a developer typed, presented as their child's school menu — including
// the allergen lists. The page's own footnote said "aligned demo data"; the
// calendar cells did not.
//
// This service is now a SHELL that reports "no menu published" for every date, and
// the calendar says so. It keeps the campus list (starter reference data) and the
// shape of the API so the real wiring — michoice-api GET /api/v1/menus/today over
// michoice-db MenuPlans, the same source michoice-office was pointed at — is a
// small change rather than a rewrite.
//
// Do not re-add a rotation. A menu nobody published is not a menu.
// ─────────────────────────────────────────────────────────────────────────────

public enum MealService { Breakfast, Lunch, Snack, AfterSchool }

public static class MealServiceX
{
    public static string Label(this MealService s) => s switch
    {
        MealService.Breakfast   => "Breakfast",
        MealService.Lunch       => "Lunch",
        MealService.Snack       => "Snack",
        MealService.AfterSchool => "After School",
        _                       => s.ToString(),
    };
    public static string Icon(this MealService s) => s switch
    {
        MealService.Breakfast   => "bi-sunrise",
        MealService.Lunch       => "bi-egg-fried",
        MealService.Snack       => "bi-apple",
        MealService.AfterSchool => "bi-moon-stars",
        _                       => "bi-egg-fried",
    };
    public static MealService[] All => new[]
        { MealService.Breakfast, MealService.Lunch, MealService.Snack, MealService.AfterSchool };
}

public class CampusInfo
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Grades { get; init; } = "";
    public string Emoji { get; init; } = "🏫";
}

public class MenuDish
{
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";      // Entrée / Side / Fruit / Drink
    public bool IsReimbursable { get; init; }         // part of the reimbursable meal
    public decimal Price { get; init; }               // à-la-carte price ($0 = in the meal)
    public int Calories { get; init; }
    public bool Vegetarian { get; init; }
    public string[] Allergens { get; init; } = System.Array.Empty<string>();

    public string Kind => IsReimbursable ? "Reimbursable meal" : "À-la-carte";
    public string PriceLabel => IsReimbursable ? "In the meal" : $"${Price:0.00}";
}

public class DailyMenu
{
    public System.DateOnly Date { get; init; }
    public MealService Service { get; init; }
    public bool IsSchoolDay { get; init; }
    public string? Title { get; init; }               // e.g. "Cheese Pizza Day"
    public System.Collections.Generic.List<MenuDish> Dishes { get; init; } = new();

    public bool HasMenu => IsSchoolDay && Dishes.Count > 0;
    public MenuDish? Entree => Dishes.Find(d => d.Category == "Entrée");
}

public class SchoolMenuService
{
    // Campus list. Starter reference data — no menus, no money, no counts.
    private readonly System.Collections.Generic.List<CampusInfo> _campuses = new()
    {
        new CampusInfo { Id = "Lincoln Elementary", Name = "Lincoln Elementary", Grades = "Grades K–5", Emoji = "🍎" },
        new CampusInfo { Id = "Jefferson Middle",   Name = "Jefferson Middle",   Grades = "Grades 6–8", Emoji = "🥪" },
        new CampusInfo { Id = "Roosevelt High",     Name = "Roosevelt High",     Grades = "Grades 9–12", Emoji = "🍕" },
    };

    public System.Collections.Generic.IReadOnlyList<CampusInfo> Campuses => _campuses;
    public CampusInfo? Campus(string? id) =>
        _campuses.Find(c => string.Equals(c.Id, id, System.StringComparison.OrdinalIgnoreCase));
    public MealService[] Services => MealServiceX.All;

    /// <summary>True if the district operates on this date (Mon–Fri).</summary>
    public bool IsSchoolDay(System.DateOnly d) =>
        d.DayOfWeek != System.DayOfWeek.Saturday && d.DayOfWeek != System.DayOfWeek.Sunday;

    /// <summary>
    /// True when this app has a menu source wired up at all. It does not — there is no
    /// michoice-api client in this project. The UI reads this to explain WHY every day is
    /// empty ("not connected yet") instead of implying the district published nothing.
    /// Flip this when the real feed lands; do not flip it to make the screen look better.
    /// </summary>
    public bool MenuFeedConnected => false;

    /// <summary>
    /// The published menu for a campus/service/date, or an empty DailyMenu when none exists.
    ///
    /// Always returns empty today: nothing publishes into this app yet. It used to return a
    /// deterministic rotation of invented dishes for every school day ever — see the header.
    /// When the michoice-api feed is wired in, query it here and return what it actually
    /// published. If it publishes nothing for a date, return empty for that date.
    /// </summary>
    public DailyMenu MenuFor(string? campusId, MealService service, System.DateOnly date) =>
        new()
        {
            Date        = date,
            Service     = service,
            IsSchoolDay = IsSchoolDay(date),
            Title       = null,
            Dishes      = new(),
        };
}
