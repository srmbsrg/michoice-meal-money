using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiChoice.MealMoney.Infrastructure.Agentic;
using MiChoice.MealMoney.Infrastructure.Data;
using MiChoice.MealMoney.Infrastructure.Identity;
using MiChoice.MealMoney.Infrastructure.Services;
using MiChoice.MealMoney.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// ─── Database — provider AND connection are 100% configuration-driven ────────────────
// Canonical section: MiChoice:Database (env MiChoice__Database__Provider /
// MiChoice__Database__ConnectionString). Legacy DatabaseProvider +
// ConnectionStrings:DefaultConnection stay honoured as a fallback.
var dbOptions = builder.Configuration
    .GetSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>() ?? new DatabaseOptions();

var dbProvider = !string.IsNullOrWhiteSpace(dbOptions.Provider)
    ? dbOptions.Provider!
    : builder.Configuration["DatabaseProvider"] ?? "Sqlite";

var useSqlServer = string.Equals(dbProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);

var connStr = !string.IsNullOrWhiteSpace(dbOptions.ConnectionString)
    ? dbOptions.ConnectionString!
    : builder.Configuration.GetConnectionString("DefaultConnection");

// Fail fast rather than silently opening a container-local file that is destroyed on
// the next redeploy — that would quietly log every parent out mid-demo.
if (string.IsNullOrWhiteSpace(connStr))
    throw new InvalidOperationException(
        $"No database connection string configured. Set '{DatabaseOptions.SectionName}:ConnectionString' " +
        "(env MiChoice__Database__ConnectionString) or ConnectionStrings:DefaultConnection.");

if (useSqlServer)
    builder.Services.AddDbContext<MealMoneyDbContext>(o => o.UseSqlServer(connStr));
else
    builder.Services.AddDbContext<MealMoneyDbContext>(o => o.UseSqlite(connStr));

// SQLite only: ensure the directory in the configured path exists, so the connection
// string can point at a mounted volume (e.g. /data/miMealMoney.db) without the image
// needing to know that path.
if (!useSqlServer)
{
    var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connStr).DataSource;
    var dir = Path.GetDirectoryName(Path.GetFullPath(dataSource));
    if (!string.IsNullOrWhiteSpace(dir))
        Directory.CreateDirectory(dir);
}

// ─── Data Protection — keys MUST outlive the container ───────────────────────────────
// ASP.NET encrypts antiforgery tokens and auth cookies with a Data Protection key ring.
// By default that ring is written to ~/.aspnet/DataProtection-Keys INSIDE the container,
// so every redeploy minted a brand-new ring and any token/cookie issued before it became
// undecryptable — "The key {...} was not found in the key ring" — which surfaced as
// HTTP 400 on every sign-in and registration POST. Persist the ring next to the database
// on the volume, and pin the application name so the ring is found again after restart.
// Path is configuration-driven (MiChoice:DataProtection:KeyPath); default sits beside
// the configured SQLite file so it follows the volume automatically.
var keyPath = builder.Configuration["MiChoice:DataProtection:KeyPath"];
if (string.IsNullOrWhiteSpace(keyPath) && !useSqlServer)
{
    var dbDir = Path.GetDirectoryName(Path.GetFullPath(
        new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connStr).DataSource));
    if (!string.IsNullOrWhiteSpace(dbDir))
        keyPath = Path.Combine(dbDir, "dp-keys");
}

if (!string.IsNullOrWhiteSpace(keyPath))
{
    Directory.CreateDirectory(keyPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
        .SetApplicationName("MiMealMoney");
}

// Identity
builder.Services
    .AddIdentity<MealMoneyUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<MealMoneyDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/auth/login";
});

// Payments: real Stripe when this install supplies live keys (per-district production install),
// otherwise the simulated stub (demo installs). Config-gated so the SAME build serves both.
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));
var stripeOptions = builder.Configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>() ?? new StripeOptions();
if (stripeOptions.IsLive)
    builder.Services.AddScoped<IPaymentService, StripePaymentService>();
else
    builder.Services.AddScoped<IPaymentService, StubPaymentService>();

// Application services
builder.Services.AddScoped<IAgenticInsightService, MealMoneyInsightService>();

// ─── Shared district system (michoice-api) — all configuration-driven ────────────────
// MiChoice:UseSharedAccounts (default true) puts student lookup + parent deposits on the
// SHARED Account.Balance the POS reads. Set it false for the legacy self-contained
// "DemoSeparate" behaviour. MiChoice:ApiBaseUrl carries the address — never hardcoded.
builder.Services.Configure<MiChoiceOptions>(builder.Configuration.GetSection(MiChoiceOptions.SectionName));
var miChoiceOptions = builder.Configuration.GetSection(MiChoiceOptions.SectionName).Get<MiChoiceOptions>()
                      ?? new MiChoiceOptions();

// Legacy key kept as a fallback so existing installs keep resolving.
var miChoiceApiBaseUrl = miChoiceOptions.ApiBaseUrl
                         ?? builder.Configuration["MiChoiceApiBaseUrl"];

if (miChoiceOptions.UseSharedAccounts && string.IsNullOrWhiteSpace(miChoiceApiBaseUrl))
    throw new InvalidOperationException(
        $"{MiChoiceOptions.SectionName}:UseSharedAccounts is true but no API base URL is configured. " +
        $"Set {MiChoiceOptions.SectionName}:ApiBaseUrl (env MiChoice__ApiBaseUrl).");

builder.Services.AddHttpClient("MiChoiceApi", c =>
{
    c.BaseAddress = new Uri(miChoiceApiBaseUrl ?? "https://api.michoice.app");
    c.Timeout     = TimeSpan.FromSeconds(20);
});
builder.Services.AddScoped<IMiChoiceAccountService, MiChoiceAccountService>();

// Parent-facing school menu source (self-contained demo data, aligned with Manager vocab)
builder.Services.AddSingleton<MiChoice.MealMoney.Web.Services.SchoolMenuService>();

// Blazor
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.Logger.LogInformation("MiMealMoney payment mode: {Mode}",
    stripeOptions.IsLive ? "LIVE Stripe (real charges)" : "Simulated stub (demo — no real charges)");

// Ensure DB exists
//
// TODO (monolith track) — MOVE THIS STORE TO EF CORE MIGRATIONS.
// EnsureCreated() creates the schema once and then never alters it. That was harmless
// while this database was wiped on every redeploy, but it now lives on a persistent
// volume, so ANY entity change from here on will NOT reach the existing database and
// the app will fail at runtime on the missing column. michoice-api already does this
// correctly (Add-Migration + Database.Migrate()); mirror that here when the monolith
// work reaches this service. Until then, an entity change requires wiping the
// database file on the volume.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MealMoneyDbContext>();
    db.Database.EnsureCreated();
    app.Logger.LogInformation("MiMealMoney database provider: {Provider}", dbProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapPost("/auth/logout", async (SignInManager<MealMoneyUser> signIn, HttpContext ctx) =>
{
    await signIn.SignOutAsync();
    ctx.Response.Redirect("/");
});

app.MapGet("/health", () => Results.Ok("Healthy"));
app.Run();