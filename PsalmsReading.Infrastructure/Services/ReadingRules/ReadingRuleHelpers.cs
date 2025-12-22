using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public static class ReadingRuleHelpers
{
    public static Psalm? SelectByTheme(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts, string value, Random random) =>
        SelectBestByTier(candidates.Where(p => p.Themes.Any(t => MatchesNormalized(t, value))), readCounts, random);

    public static Psalm? SelectByTypeThemeOrEpigraph(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts, string value, Random random)
    {
        IEnumerable<Psalm> psalms = candidates as Psalm[] ?? candidates.ToArray();
        IEnumerable<Psalm> byType = psalms.Where(p => MatchesNormalized(p.Type, value));
        Psalm? selected = SelectBestByTier(byType, readCounts, random);
        if (selected is not null)
        {
            return selected;
        }

        IEnumerable<Psalm> byTheme = psalms.Where(p => p.Themes.Any(t => MatchesNormalized(t, value)));
        selected = SelectBestByTier(byTheme, readCounts, random);
        if (selected is not null)
        {
            return selected;
        }

        IEnumerable<Psalm> byEpigraph = psalms.Where(p => p.Epigraphs.Any(e => MatchesNormalized(e, value)));
        return SelectBestByTier(byEpigraph, readCounts, random);
    }

    public static Psalm? SelectBestByTier(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts, Random random)
    {
        List<Psalm> candidateList = candidates.ToList();
        if (candidateList.Count == 0)
        {
            return null;
        }

        List<IGrouping<int, Psalm>> groupedByReadCount = candidateList
            .GroupBy(p => readCounts.GetValueOrDefault(p.Id))
            .OrderBy(g => g.Key)
            .ToList();

        foreach (List<Psalm> tierList in groupedByReadCount.Select(tier => tier.ToList()).Where(tierList => tierList.Count != 0))
        {
            if (tierList.Count <= 2)
            {
                return tierList[0];
            }

            int randomIndex = random.Next(tierList.Count);
            return tierList[randomIndex];
        }

        return null;
    }

    public static Psalm? SelectByTypeBalance(
        IEnumerable<Psalm> candidates,
        IReadOnlyDictionary<int, int> readCounts,
        IReadOnlyDictionary<string, TypeBalanceStats> typeBalances,
        IReadOnlyDictionary<string, int> recentTypeCounts,
        int recentTotalCount,
        Random random)
    {
        List<Psalm> candidateList = candidates.ToList();
        if (candidateList.Count == 0)
        {
            return null;
        }

        List<IGrouping<string?, Psalm>> byType = candidateList
            .GroupBy(p => p.Type?.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        double maxScore = byType
            .Select(group => GetTypeScore(group.Key, typeBalances, recentTypeCounts, recentTotalCount))
            .DefaultIfEmpty(0d)
            .Max();

        List<IGrouping<string?, Psalm>> topGroups = byType
            .Where(group => Math.Abs(GetTypeScore(group.Key, typeBalances, recentTypeCounts, recentTotalCount) - maxScore) < 0.0001d)
            .ToList();

        IGrouping<string?, Psalm> selectedGroup = topGroups.Count <= 1
            ? topGroups[0]
            : topGroups[random.Next(topGroups.Count)];

        return SelectBestByTier(selectedGroup, readCounts, random);
    }

    private static double GetTypeScore(
        string? type,
        IReadOnlyDictionary<string, TypeBalanceStats> typeBalances,
        IReadOnlyDictionary<string, int> recentTypeCounts,
        int recentTotalCount)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return 0d;
        }

        string key = type.Trim();
        double remainingRatio = typeBalances.TryGetValue(key, out TypeBalanceStats? stats)
            ? stats.RemainingRatio
            : 0d;
        double recentRatio = 0d;

        if (recentTotalCount > 0 && recentTypeCounts.TryGetValue(key, out int recentCount))
        {
            recentRatio = (double)recentCount / recentTotalCount;
        }

        return remainingRatio - recentRatio;
    }

    private static bool MatchesNormalized(string? source, string target) => !string.IsNullOrWhiteSpace(source) && string.Equals(Normalize(source), Normalize(target), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value)
    {
        string normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        IEnumerable<char> filtered = normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(filtered.ToArray()).ToLowerInvariant().Trim();
    }
}
