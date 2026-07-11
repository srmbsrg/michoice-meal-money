using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiChoice.MealMoney.Infrastructure.Agentic;
using MiChoice.MealMoney.Infrastructure.Data;
using MiChoice.MealMoney.Infrastructure.Identity;
using MiChoice.MealMoney.Infrastructure.Services;
using MiChoice.MealMoney.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;

if (dbProvider == "SqlServer")
    builder.Services.AddDbContext<MealMoneyDbContext>(o => o.UseSqlServer(connStr));
else
    builder.Services.AddDbContext<MealMoneyDbContext>(o => o.UseSqlite(connStr));

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
builder.Services.AddHttpClient("MiChoiceApi", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["MiChoiceApiBaseUrl"] ?? "https://api.michoice.app");
});

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
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MealMoneyDbContext>();
    db.Database.EnsureCreated();
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