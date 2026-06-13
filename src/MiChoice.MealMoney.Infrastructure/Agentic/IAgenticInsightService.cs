namespace MiChoice.MealMoney.Infrastructure.Agentic;

public interface IAgenticInsightService
{
    Task<AgenticInsight?> GetAccountInsightAsync(
        int accountId,
        decimal balance,
        IEnumerable<decimal> recentDailySpend,
        CancellationToken ct = default);
}

public record AgenticInsight(string Summary, InsightSeverity Severity, string? ActionLabel, string? ActionUrl);

public enum InsightSeverity { Info, Warning, Action }
