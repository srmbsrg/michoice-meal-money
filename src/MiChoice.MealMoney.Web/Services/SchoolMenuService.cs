namespace MiChoice.MealMoney.Web.Services;

// ─────────────────────────────────────────────────────────────────────────────
// SchoolMenuService — the PARENT-FACING menu source (V, 2026-07-03).
// Self-contained, read-only, in-memory demo data (singleton). No DB / no gateway.
//
// The vocabulary here is deliberately ALIGNED with the MiChoice MANAGER module
// (MiChoice.Office.Web ManagerStore): same campuses (Lincoln Elementary /
// Jefferson Middle / Roosevelt High), same meal services (Breakfast / Lunch /
// Snack / After School) and the same reimbursable-vs-à-la-carte item model — so
// the story "a menu configured in Manager shows up here for parents" holds
// visually end-to-end.
//
// HONEST SCOPE: true real-time cross-app menu sync needs the shared gateway /
// backend (roadmap). For the demo this is aligned demo data, generated with a
// deterministic per-campus rotation so every school day of any month is filled.
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
    // Campuses — SAME names Manager (MiChoice.Office.Web) configures.
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

    // A school day = Mon–Fri (weekends closed in the demo).
    public bool IsSchoolDay(System.DateOnly d) =>
        d.DayOfWeek != System.DayOfWeek.Saturday && d.DayOfWeek != System.DayOfWeek.Sunday;

    // ---- dish factories (à-la-carte prices aligned with Manager seed items) ----
    private static MenuDish Meal(string name, int cal, bool veg, params string[] allergens) =>
        new() { Name = name, Category = "Entrée", IsReimbursable = true, Price = 0m, Calories = cal, Vegetarian = veg, Allergens = allergens };
    private static MenuDish Side(string name, int cal, bool veg, params string[] allergens) =>
        new() { Name = name, Category = "Side", IsReimbursable = true, Price = 0m, Calories = cal, Vegetarian = veg, Allergens = allergens };
    private static MenuDish Fruit(string name, int cal) =>
        new() { Name = name, Category = "Fruit", IsReimbursable = true, Price = 0m, Calories = cal, Vegetarian = true };
    private static MenuDish Milk =>
        new() { Name = "Milk", Category = "Drink", IsReimbursable = true, Price = 0.50m, Calories = 100, Vegetarian = true, Allergens = new[] { "Milk" } };
    private static MenuDish Alc(string name, string cat, decimal price, int cal, bool veg, params string[] allergens) =>
        new() { Name = name, Category = cat, IsReimbursable = false, Price = price, Calories = cal, Vegetarian = veg, Allergens = allergens };

    private sealed record Day(string Title, System.Func<System.Collections.Generic.List<MenuDish>> Build);

    // shared à-la-carte extras offered every day (matches Manager: Milk/Cookie/Water)
    private static void AddExtras(System.Collections.Generic.List<MenuDish> d)
    {
        d.Add(Alc("Cookie", "Snack", 1.00m, 160, true, "Wheat", "Egg", "Milk"));
        d.Add(Alc("Bottled Water", "Drink", 1.25m, 0, true));
    }

    // ---- BREAKFAST rotation ----
    private static readonly Day[] Breakfasts =
    {
        new("Warm Pancakes", () => new() { Meal("Whole-Grain Pancakes & Syrup", 290, true, "Wheat", "Egg", "Milk"), Fruit("Fresh Banana", 90), Milk }),
        new("Breakfast Burrito", () => new() { Meal("Egg & Cheese Breakfast Burrito", 320, true, "Wheat", "Egg", "Milk"), Fruit("Diced Peaches", 60), Milk }),
        new("Cereal & Yogurt", () => new() { Meal("Whole-Grain Cereal", 210, true, "Wheat"), Side("Vanilla Yogurt Cup", 90, true, "Milk"), Fruit("Fresh Apple", 80), Milk }),
        new("Bagel Morning", () => new() { Meal("Whole-Grain Bagel & Cream Cheese", 300, true, "Wheat", "Milk"), Fruit("Orange Wedges", 70), Milk }),
        new("Oatmeal Bar", () => new() { Meal("Cinnamon Oatmeal", 230, true, "Wheat"), Side("Blueberry Muffin", 190, true, "Wheat", "Egg", "Milk"), Fruit("Fresh Banana", 90), Milk }),
    };

    // ---- LUNCH rotation (entrée names align with typical Manager 'Reimbursable Lunch') ----
    private static readonly Day[] Lunches =
    {
        new("Cheese Pizza Day", () => new() { Meal("Whole-Grain Cheese Pizza", 350, true, "Wheat", "Milk"), Side("Garden Salad", 35, true), Fruit("Fresh Apple", 80), Milk }),
        new("Chicken Nuggets", () => new() { Meal("Baked Chicken Nuggets", 300, false, "Wheat"), Side("Roasted Corn", 80, true), Fruit("Pineapple Cup", 70), Milk }),
        new("Taco Tuesday", () => new() { Meal("Beef & Cheese Soft Taco", 340, false, "Wheat", "Milk"), Side("Seasoned Black Beans", 110, true), Fruit("Orange Wedges", 70), Milk }),
        new("Turkey Sandwich", () => new() { Meal("Turkey & Cheese Sandwich", 330, false, "Wheat", "Milk"), Side("Baby Carrots & Ranch", 90, true, "Milk"), Fruit("Diced Pears", 60), Milk }),
        new("Spaghetti Day", () => new() { Meal("Spaghetti with Marinara", 360, true, "Wheat"), Side("Steamed Green Beans", 45, true), Fruit("Fresh Banana", 90), Milk }),
    };

    // ---- SNACK rotation ----
    private static readonly Day[] Snacks =
    {
        new("Fruit & Cheese", () => new() { Meal("Apple Slices & Cheese Stick", 150, true, "Milk"), Milk }),
        new("Graham & Yogurt", () => new() { Meal("Graham Crackers & Yogurt", 180, true, "Wheat", "Milk"), Fruit("Raisins", 90) }),
        new("Veggie Cup", () => new() { Meal("Veggie Cup & Hummus", 140, true, "Wheat"), Fruit("Fresh Orange", 70) }),
    };

    // ---- AFTER SCHOOL rotation ----
    private static readonly Day[] AfterSchools =
    {
        new("Supper: Chicken Wrap", () => new() { Meal("Grilled Chicken Wrap", 340, false, "Wheat", "Milk"), Side("Cucumber Slices", 20, true), Fruit("Fresh Apple", 80), Milk }),
        new("Supper: Cheese Quesadilla", () => new() { Meal("Cheese Quesadilla", 320, true, "Wheat", "Milk"), Side("Salsa & Corn", 90, true), Fruit("Orange Wedges", 70), Milk }),
        new("Supper: Sun Butter Sandwich", () => new() { Meal("Sun Butter & Jelly Sandwich", 360, true, "Wheat"), Side("Celery Sticks", 15, true), Fruit("Fresh Banana", 90), Milk }),
    };

    private static Day[] Rotation(MealService s) => s switch
    {
        MealService.Breakfast   => Breakfasts,
        MealService.Lunch       => Lunches,
        MealService.Snack       => Snacks,
        MealService.AfterSchool => AfterSchools,
        _                       => Lunches,
    };

    private int CampusOffset(string? campusId)
    {
        var idx = _campuses.FindIndex(c => string.Equals(c.Id, campusId, System.StringComparison.OrdinalIgnoreCase));
        return idx < 0 ? 0 : idx;
    }

    // Deterministic per-campus, per-service, per-date lookup — every school day filled.
    public DailyMenu MenuFor(string? campusId, MealService service, System.DateOnly date)
    {
        if (!IsSchoolDay(date))
            return new DailyMenu { Date = date, Service = service, IsSchoolDay = false };

        var rot = Rotation(service);
        var offset = CampusOffset(campusId) * 2 + (int)service;
        var i = ((date.DayOfYear + offset) % rot.Length + rot.Length) % rot.Length;
        var day = rot[i];
        var dishes = day.Build();
        AddExtras(dishes);
        return new DailyMenu
        {
            Date = date,
            Service = service,
            IsSchoolDay = true,
            Title = day.Title,
            Dishes = dishes,
        };
    }
}
