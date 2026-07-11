using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiChoice.MealMoney.Domain.Enums;
using Stripe;

namespace MiChoice.MealMoney.Infrastructure.Services;

/// <summary>
/// Real Stripe payment processing via the PaymentIntents API. Selected by DI when a live install
/// supplies real Stripe keys (<see cref="StripeOptions.IsLive"/>); demo installs use
/// <see cref="StubPaymentService"/> instead.
///
/// Flow (Payment Element): the browser calls <see cref="CreatePaymentIntentAsync"/> to open a
/// PaymentIntent and mounts Stripe's Payment Element with the returned client secret; the card is
/// entered and confirmed entirely client-side (PCI-safe — the server never sees a PAN). The server
/// then calls <see cref="ConfirmPaymentIntentAsync"/> to RE-FETCH the intent from Stripe and verify
/// it genuinely succeeded before any balance is credited — the client's word is never trusted.
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly ILogger<StripePaymentService> _logger;
    private readonly StripeOptions _options;
    private readonly PaymentIntentService _paymentIntents;

    public StripePaymentService(ILogger<StripePaymentService> logger, IOptions<StripeOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        var client = new StripeClient(_options.SecretKey);
        _paymentIntents = new PaymentIntentService(client);
    }

    public bool IsLive => true;
    public string? PublishableKey => _options.PublishableKey;

    public async Task<PaymentIntentInit> CreatePaymentIntentAsync(PaymentIntentInitRequest request, CancellationToken ct = default)
    {
        var amountInCents = (long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero);
        if (amountInCents <= 0)
            return new PaymentIntentInit(false, null, null, null, "Amount must be greater than zero.");

        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = _options.Currency,
                // Card-only, user-present: no redirect-based methods, so confirmation completes inline.
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never",
                },
                Metadata = new Dictionary<string, string>
                {
                    ["mealMoneyAccountId"] = request.MealMoneyAccountId.ToString(),
                    ["parentUserId"] = request.ParentUserId,
                },
            };

            var intent = await _paymentIntents.CreateAsync(options, cancellationToken: ct);
            return new PaymentIntentInit(true, intent.ClientSecret, intent.Id, _options.PublishableKey, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe CreatePaymentIntent failed for account {AccountId}: {Message}",
                request.MealMoneyAccountId, ex.StripeError?.Message ?? ex.Message);
            return new PaymentIntentInit(false, null, null, null, ex.StripeError?.Message ?? "Could not start payment.");
        }
    }

    public async Task<PaymentResult> ConfirmPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            var intent = await _paymentIntents.GetAsync(
                paymentIntentId,
                new PaymentIntentGetOptions { Expand = new List<string> { "payment_method" } },
                cancellationToken: ct);

            return MapIntent(intent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe ConfirmPaymentIntent failed for {IntentId}: {Message}",
                paymentIntentId, ex.StripeError?.Message ?? ex.Message);
            return new PaymentResult(false, PaymentStatus.Failed, paymentIntentId, null, CardBrand.Unknown,
                ex.StripeError?.Message ?? "Payment could not be verified.");
        }
    }

    // Not used in live mode — the UI uses the Payment Element (Create + Confirm) rather than
    // posting raw card data. Present only to satisfy the interface.
    public Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken ct = default)
        => throw new NotSupportedException("Live Stripe mode uses the Payment Element flow (CreatePaymentIntentAsync/ConfirmPaymentIntentAsync).");

    private static PaymentResult MapIntent(PaymentIntent intent)
    {
        var card = intent.PaymentMethod?.Card;
        var brand = MapBrand(card?.Brand);
        var last4 = card?.Last4;

        var (success, status, error) = intent.Status switch
        {
            "succeeded" => (true, PaymentStatus.Completed, (string?)null),
            "processing" => (true, PaymentStatus.Pending, null),
            _ => (false, PaymentStatus.Failed, $"Payment not completed (status: {intent.Status})."),
        };

        return new PaymentResult(success, status, intent.Id, last4, brand, error);
    }

    private static CardBrand MapBrand(string? stripeBrand) => stripeBrand switch
    {
        "visa" => CardBrand.Visa,
        "mastercard" => CardBrand.Mastercard,
        "amex" => CardBrand.AmericanExpress,
        "discover" => CardBrand.Discover,
        _ => CardBrand.Unknown,
    };
}
