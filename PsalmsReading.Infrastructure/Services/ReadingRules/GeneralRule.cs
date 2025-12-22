using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public sealed class GeneralRule : IReadingRule
{
    private const int MonthlyTypeCapHigh = 2;
    private const int MonthlyTypeCapLow = 1;
    private const double HighVolumeThreshold = 0.6d;
    private static readonly string[] PriorityTypes = ["alabanza", "lamento"];

    public string Name => "General";

    public bool CanApply(ScheduleContext context) => true;

    public Psalm? Select(ScheduleContext context)
    {
        IReadOnlyList<Psalm> candidates = context.AvailablePsalms;
        if (candidates.Count == 0)
        {
            return null;
        }

        List<Psalm> streakFiltered = FilterByLastType(candidates, context.RecentTypes);
        if (streakFiltered.Count == 0)
        {
            streakFiltered = candidates.ToList();
        }

        List<Psalm> monthCapped = streakFiltered
            .Where(psalm => !IsMonthCapped(psalm.Type, context.MonthTypeCounts, context.TypeBalances, context.MaxTypeTotalReadable))
            .ToList();

        if (monthCapped.Count == 0)
        {
            monthCapped = streakFiltered;
        }

        List<Psalm> priorityCandidates = GetPriorityCandidates(monthCapped, context.MonthTypeCounts);
        IReadOnlyList<Psalm> selectionPool = priorityCandidates.Count > 0 ? priorityCandidates : monthCapped;

        return ReadingRuleHelpers.SelectByTypeBalance(
            selectionPool,
            context.ReadCounts,
            context.TypeBalances,
            context.MaxTypeTotalReadable,
            context.RecentTypeCounts,
            context.RecentTotalCount,
            context.Random);
    }

    private static bool IsMonthCapped(
        string? type,
        IReadOnlyDictionary<string, int> monthTypeCounts,
        IReadOnlyDictionary<string, TypeBalanceStats> typeBalances,
        int maxTypeTotalReadable)
    {
        string key = NormalizeType(type);
        int cap = GetMonthlyCap(key, typeBalances, maxTypeTotalReadable);
        return monthTypeCounts.TryGetValue(key, out int count) && count >= cap;
    }

    private static int GetMonthlyCap(
        string typeKey,
        IReadOnlyDictionary<string, TypeBalanceStats> typeBalances,
        int maxTypeTotalReadable)
    {
        if (!typeBalances.TryGetValue(typeKey, out TypeBalanceStats? stats) || maxTypeTotalReadable <= 0)
        {
            return MonthlyTypeCapLow;
        }

        double ratio = (double)stats.TotalReadable / maxTypeTotalReadable;
        return ratio >= HighVolumeThreshold ? MonthlyTypeCapHigh : MonthlyTypeCapLow;
    }

    private static List<Psalm> GetPriorityCandidates(
        IReadOnlyList<Psalm> candidates,
        IReadOnlyDictionary<string, int> monthTypeCounts)
    {
        List<string> missing = PriorityTypes
            .Where(type => !monthTypeCounts.ContainsKey(type))
            .ToList();

        if (missing.Count == 0)
        {
            return new List<Psalm>();
        }

        List<Psalm> priority = candidates
            .Where(psalm => missing.Contains(NormalizeType(psalm.Type), StringComparer.OrdinalIgnoreCase))
            .ToList();

        return priority;
    }

    private static string NormalizeType(string? type) =>
        string.IsNullOrWhiteSpace(type) ? string.Empty : type.Trim().ToLowerInvariant();

    private static List<Psalm> FilterByLastType(IReadOnlyList<Psalm> candidates, IReadOnlyList<string> recentTypes)
    {
        if (recentTypes.Count == 0)
        {
            return candidates.ToList();
        }

        string last = recentTypes[^1];
        return candidates
            .Where(psalm => !string.Equals(NormalizeType(psalm.Type), last, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
