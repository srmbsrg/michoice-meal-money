namespace MiChoice.MealMoney.Infrastructure.Agentic;

/// <summary>
/// Pure-math stub: projects days remaining based on average daily spend.
/// No external API call required.
/// </summary>
public class MealMoneyInsightService : IAgenticInsightService
{
    public Task<AgenticInsight?> GetAccountInsightAsync(
        int accountId,
        decimal balance,
        IEnumerable<decimal> recentDailySpend,
        CancellationToken ct = default)
    {
        var spends = recentDailySpend.ToList();

        if (spends.Count == 0)
        {
            var insight = new AgenticInsight(
                Summary: "No recent transaction data available to project balance.",
                Severity: InsightSeverity.Info,
                ActionLabel: null,
                ActionUrl: null);
            return Task.FromResult<AgenticInsight?>(insight);
        }

        var avgDailySpend = spends.Average();

        if (avgDailySpend <= 0)
        {
            var insight = new AgenticInsight(
                Summary: "No spending detected in recent history.",
                Severity: InsightSeverity.Info,
                ActionLabel: null,
                ActionUrl: null);
            return Task.FromResult<AgenticInsight?>(insight);
        }

        var daysRemaining = (int)Math.Floor(balance / avgDailySpend);

        AgenticInsight result;

        if (balance < 0)
        {
            result = new AgenticInsight(
                Summary: $"This account has a negative balance of {balance:C}. Please add funds immediately to avoid meal service interruption.",
                Severity: InsightSeverity.Action,
                ActionLabel: "Add Funds Now",
                ActionUrl: "/pay");
        }
        else if (daysRemaining <= 3)
        {
            result = new AgenticInsight(
                Summary: $"At {avgDailySpend:C}/day average spend, this account will reach $0 in approximately {daysRemaining} school day{(daysRemaining == 1 ? "" : "s")}. Consider adding funds soon.",
                Severity: InsightSeverity.Action,
                ActionLabel: "Add Funds",
                ActionUrl: "/pay");
        }
        else if (daysRemaining <= 8)
        {
            result = new AgenticInsight(
                Summary: $"At {avgDailySpend:C}/day average spend, this account will reach $0 in approximately {daysRemaining} school days. Consider adding ${Math.Ceiling(avgDailySpend * 10):N0} to cover the next two weeks.",
                Severity: InsightSeverity.Warning,
                ActionLabel: "Add Funds",
                ActionUrl: "/pay");
        }
        else
        {
            result = new AgenticInsight(
                Summary: $"At {avgDailySpend:C}/day average spend, this account has approximately {daysRemaining} school days of funds remaining. Current balance: {balance:C}.",
                Severity: InsightSeverity.Info,
                ActionLabel: null,
                ActionUrl: null);
        }

        return Task.FromResult<AgenticInsight?>(result);
    }
}
