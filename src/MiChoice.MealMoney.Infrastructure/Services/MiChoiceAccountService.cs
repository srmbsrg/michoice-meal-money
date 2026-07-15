using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MiChoice.MealMoney.Infrastructure.Services;

/// <summary>
/// michoice-api client for shared accounts + deposits.
///
/// Deposits are posted as a ledger transaction (MealType.Deposit = 4) rather than a
/// direct balance write: the API applies it inside a DB transaction that records the
/// Transaction row AND updates Account.Balance together, so the parent payment and the
/// register's view of the money can never drift apart.
/// </summary>
public sealed class MiChoiceAccountService : IMiChoiceAccountService
{
    /// <summary>Mirrors MiChoice.Shared.Enums.MealType.Deposit on the API side.</summary>
    private const int MealTypeDeposit = 4;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _factory;
    private readonly ILogger<MiChoiceAccountService> _logger;

    public MiChoiceAccountService(IHttpClientFactory factory, ILogger<MiChoiceAccountService> logger)
    {
        _factory = factory;
        _logger  = logger;
    }

    private HttpClient Client => _factory.CreateClient("MiChoiceApi");

    public async Task<IReadOnlyList<SharedAccount>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SharedAccount>();

        try
        {
            var dtos = await Client.GetFromJsonAsync<List<AccountDto>>(
                $"api/accounts?q={Uri.EscapeDataString(query)}", Json, ct);

            return dtos?.Select(Map).ToList() ?? new List<SharedAccount>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shared account search failed for query {Query}", query);
            return Array.Empty<SharedAccount>();
        }
    }

    public async Task<SharedAccount?> GetAsync(string accountNumber, CancellationToken ct = default)
    {
        try
        {
            var dto = await Client.GetFromJsonAsync<AccountDto>(
                $"api/accounts/{Uri.EscapeDataString(accountNumber)}", Json, ct);
            return dto is null ? null : Map(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shared account fetch failed for {AccountNumber}", accountNumber);
            return null;
        }
    }

    public async Task<DepositResult> DepositAsync(
        string accountNumber, decimal amount, Guid clientTransactionId, CancellationToken ct = default)
    {
        try
        {
            // ClientTransactionId makes the post idempotent-friendly: a retry carries the
            // same id rather than double-crediting the student.
            var payload = new
            {
                accountNumber       = accountNumber,
                amount              = amount,
                transactionType     = MealTypeDeposit,
                mealService         = "MealMoney",
                registerNumber      = 0,
                campus              = 0,
                clientTransactionId = clientTransactionId,
                timestamp           = DateTime.UtcNow
            };

            var response = await Client.PostAsJsonAsync("api/transactions", payload, Json, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Shared deposit failed for {AccountNumber}: HTTP {Status} {Body}",
                    accountNumber, (int)response.StatusCode, body);
                return new DepositResult(false, 0m, $"Deposit rejected by MiChoice API ({(int)response.StatusCode}).");
            }

            var tx = await response.Content.ReadFromJsonAsync<TransactionDto>(Json, ct);
            var newBalance = tx?.BalanceAfter ?? 0m;

            _logger.LogInformation(
                "Shared deposit posted: {Amount:C} to {AccountNumber} — new shared balance {Balance:C}",
                amount, accountNumber, newBalance);

            return new DepositResult(true, newBalance, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shared deposit threw for {AccountNumber}", accountNumber);
            return new DepositResult(false, 0m, "Could not reach the MiChoice service to post your payment.");
        }
    }

    private static SharedAccount Map(AccountDto a) => new(
        a.AccountNumber, a.First, a.Last, a.Grade, a.Campus, a.HomeRoom, a.Balance);

    // Local mirrors of the API's response shapes — only the fields miMealMoney needs.
    private sealed class AccountDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string First         { get; set; } = string.Empty;
        public string Last          { get; set; } = string.Empty;
        public string Grade         { get; set; } = string.Empty;
        public int    Campus        { get; set; }
        public string HomeRoom      { get; set; } = string.Empty;
        public decimal Balance      { get; set; }
    }

    private sealed class TransactionDto
    {
        [JsonPropertyName("balanceAfter")]
        public decimal BalanceAfter { get; set; }
    }
}
