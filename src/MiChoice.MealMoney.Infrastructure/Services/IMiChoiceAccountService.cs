namespace MiChoice.MealMoney.Infrastructure.Services;

/// <summary>A student account as it exists in the SHARED district database.</summary>
/// <param name="AccountNumber">
/// The shared-identity join key. Unique-indexed on Account, carried on every
/// Transaction, and what the POS keys on — so it is the anchor miMealMoney links to.
/// </param>
public sealed record SharedAccount(
    string AccountNumber,
    string First,
    string Last,
    string Grade,
    int    Campus,
    string HomeRoom,
    decimal Balance);

/// <summary>Result of a deposit against the shared account.</summary>
public sealed record DepositResult(bool Success, decimal NewBalance, string? Error);

/// <summary>
/// Talks to michoice-api about SHARED accounts and money. miMealMoney keeps its own
/// parent identity/household records locally; only ACCOUNT / BALANCE / TRANSACTION
/// cross over to the shared system.
/// </summary>
public interface IMiChoiceAccountService
{
    /// <summary>Search shared student accounts by name or account number.</summary>
    Task<IReadOnlyList<SharedAccount>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>Fetch one shared account by its account number (authoritative balance).</summary>
    Task<SharedAccount?> GetAsync(string accountNumber, CancellationToken ct = default);

    /// <summary>
    /// Credit a parent deposit to the SHARED balance. Posts to the transaction ledger,
    /// which atomically updates Account.Balance — the same value the register reads.
    /// </summary>
    Task<DepositResult> DepositAsync(string accountNumber, decimal amount, Guid clientTransactionId, CancellationToken ct = default);
}
