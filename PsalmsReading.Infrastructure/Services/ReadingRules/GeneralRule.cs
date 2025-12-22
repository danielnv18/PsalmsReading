using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public sealed class GeneralRule : IReadingRule
{
    private const int MonthlyTypeCap = 2;
    private const double RemainingRatioThreshold = 0.5d;

    public string Name => "General";

    public bool CanApply(ScheduleContext context) => true;

    public Psalm? Select(ScheduleContext context)
    {
        IReadOnlyList<Psalm> candidates = context.AvailablePsalms;
        if (candidates.Count == 0)
        {
            return null;
        }

        var monthCapped = candidates
            .Where(psalm => !IsMonthCapped(psalm.Type, context.MonthTypeCounts))
            .ToList();

        if (monthCapped.Count == 0)
        {
            monthCapped = candidates.ToList();
        }

        var preferred = monthCapped
            .Where(psalm => IsPreferredMonthlyType(psalm.Type, context.MonthTypeCounts, context.TypeBalances))
            .ToList();

        IReadOnlyList<Psalm> selectionPool = preferred.Count > 0 ? preferred : monthCapped;

        return ReadingRuleHelpers.SelectByTypeBalance(
            selectionPool,
            context.ReadCounts,
            context.TypeBalances,
            context.RecentTypeCounts,
            context.RecentTotalCount,
            context.Random);
    }

    private static bool IsMonthCapped(string? type, IReadOnlyDictionary<string, int> monthTypeCounts)
    {
        var key = NormalizeType(type);
        return monthTypeCounts.TryGetValue(key, out int count) && count >= MonthlyTypeCap;
    }

    private static bool IsPreferredMonthlyType(
        string? type,
        IReadOnlyDictionary<string, int> monthTypeCounts,
        IReadOnlyDictionary<string, TypeBalanceStats> typeBalances)
    {
        var key = NormalizeType(type);
        if (monthTypeCounts.ContainsKey(key))
        {
            return false;
        }

        return typeBalances.TryGetValue(key, out TypeBalanceStats? stats) &&
               stats.RemainingRatio >= RemainingRatioThreshold;
    }

    private static string NormalizeType(string? type) =>
        string.IsNullOrWhiteSpace(type) ? string.Empty : type.Trim();
}
