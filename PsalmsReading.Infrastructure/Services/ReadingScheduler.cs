using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain;
using PsalmsReading.Domain.Entities;
using PsalmsReading.Infrastructure.Services.ReadingRules;

namespace PsalmsReading.Infrastructure.Services;

public sealed class ReadingScheduler(
    IPsalmRepository psalmRepository,
    IReadingRepository readingRepository,
    IPlannedReadingRepository plannedRepository,
    Random? random = null)
    : IReadingScheduler
{
    private static readonly HashSet<int> HolyWeekPreferredIds = [113, 114, 115, 116, 117, 118];
    private const int RollingTypeWindowDays = 42;

    private readonly Random _random = random ?? new Random();
    private readonly IReadOnlyList<IReadingRule> _rules =
    [
        new FirstSundayOfYearRule(),
        new ThanksgivingRule(),
        new HolyWeekRule(HolyWeekPreferredIds),
        new DecemberRule(),
        new FirstSundayPraiseRule(),
        new GeneralRule()
    ];

    public async Task<IReadOnlyList<PlannedReading>> GenerateScheduleAsync(DateOnly startDate, int months, CancellationToken cancellationToken = default)
    {
        if (months <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(months), "Months must be positive.");
        }

        IReadOnlyDictionary<int, Psalm> psalms = (await psalmRepository.GetAllAsync(cancellationToken))
            .Where(PsalmRules.IsReadable)
            .ToDictionary(p => p.Id);

        IReadOnlyDictionary<int, int> readCounts = (await readingRepository.GetAllAsync(cancellationToken))
            .GroupBy(r => r.PsalmId)
            .ToDictionary(g => g.Key, g => g.Count());

        IReadOnlyDictionary<string, TypeBalanceStats> typeBalances = BuildTypeBalances(psalms.Values, readCounts);
        int maxTypeTotalReadable = typeBalances.Values.Select(stats => stats.TotalReadable).DefaultIfEmpty(0).Max();

        IReadOnlyList<PlannedReading> plannedAll = await plannedRepository.GetAllAsync(cancellationToken);
        List<TypeHistoryEntry> plannedHistory = BuildPlannedHistory(plannedAll, psalms);
        Dictionary<string, Dictionary<string, int>> monthTypeCounts = BuildMonthTypeCounts(plannedHistory);

        DateOnly endDate = startDate.AddMonths(months);
        List<DateOnly> sundays = EnumerateSundays(startDate, endDate).ToList();
        HashSet<DateOnly> holyWeekSundays = GetHolyWeekSundays(sundays);
        HashSet<DateOnly> thanksgivingSundays = GetLastTwoSundaysOfNovember(sundays);

        List<PlannedReading> planned = new();
        HashSet<int> usedPsalmIds = new();

        int plannedIndex = 0;
        List<TypeHistoryEntry> rollingHistory = new();

        foreach (DateOnly sunday in sundays)
        {
            while (plannedIndex < plannedHistory.Count && plannedHistory[plannedIndex].Date < sunday)
            {
                rollingHistory.Add(plannedHistory[plannedIndex]);
                plannedIndex++;
            }

            IReadOnlyDictionary<string, int> recentTypeCounts = GetRecentTypeCounts(rollingHistory, sunday);
            int recentTotalCount = recentTypeCounts.Values.Sum();
            IReadOnlyList<string> recentTypes = GetRecentTypes(rollingHistory);
            Dictionary<string, int> monthCounts = GetMonthCounts(monthTypeCounts, sunday);

            List<Psalm> available = psalms.Values.Where(p => !usedPsalmIds.Contains(p.Id)).ToList();
            if (available.Count == 0)
            {
                break;
            }

            ScheduleContext context = new(
                sunday,
                available,
                readCounts,
                typeBalances,
                maxTypeTotalReadable,
                recentTypeCounts,
                recentTotalCount,
                monthCounts,
                recentTypes,
                holyWeekSundays.Contains(sunday),
                thanksgivingSundays.Contains(sunday),
                sunday.Month == 12,
                IsFirstSundayOfMonth(sunday),
                IsFirstSundayOfYear(sunday),
                _random);

            (Psalm? chosen, string appliedRule) = ApplyRules(context);

            if (chosen is null)
            {
                continue;
            }

            usedPsalmIds.Add(chosen.Id);
            planned.Add(new PlannedReading(Guid.NewGuid(), chosen.Id, sunday, appliedRule));
            rollingHistory.Add(new TypeHistoryEntry(sunday, NormalizeType(chosen.Type)));
            IncrementMonthCount(monthTypeCounts, sunday, NormalizeType(chosen.Type));
        }

        return planned;
    }

    private static IReadOnlyDictionary<string, TypeBalanceStats> BuildTypeBalances(IEnumerable<Psalm> psalms, IReadOnlyDictionary<int, int> readCounts)
    {
        Dictionary<string, TypeBalanceStats> balances = new(StringComparer.OrdinalIgnoreCase);
        foreach (IGrouping<string, Psalm> group in psalms.GroupBy(p => NormalizeType(p.Type), StringComparer.OrdinalIgnoreCase))
        {
            string type = group.Key.Trim();
            int totalReadable = group.Count();
            int coverageCount = group.Count(psalm => readCounts.GetValueOrDefault(psalm.Id) > 0);
            balances[type] = new TypeBalanceStats(type, totalReadable, coverageCount);
        }

        return balances;
    }

    private static List<TypeHistoryEntry> BuildPlannedHistory(IReadOnlyList<PlannedReading> planned, IReadOnlyDictionary<int, Psalm> psalms)
    {
        return planned
            .Where(p => psalms.TryGetValue(p.PsalmId, out _))
            .Select(p => new TypeHistoryEntry(p.ScheduledDate, NormalizeType(psalms[p.PsalmId].Type)))
            .OrderBy(entry => entry.Date)
            .ToList();
    }

    private static Dictionary<string, Dictionary<string, int>> BuildMonthTypeCounts(IEnumerable<TypeHistoryEntry> history)
    {
        Dictionary<string, Dictionary<string, int>> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (TypeHistoryEntry entry in history)
        {
            string monthKey = GetMonthKey(entry.Date);
            if (!result.TryGetValue(monthKey, out Dictionary<string, int>? typeCounts))
            {
                typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                result[monthKey] = typeCounts;
            }

            typeCounts[entry.Type] = typeCounts.GetValueOrDefault(entry.Type) + 1;
        }

        return result;
    }

    private static Dictionary<string, int> GetMonthCounts(
        Dictionary<string, Dictionary<string, int>> monthTypeCounts,
        DateOnly date)
    {
        string monthKey = GetMonthKey(date);
        if (monthTypeCounts.TryGetValue(monthKey, out Dictionary<string, int>? counts))
        {
            return counts;
        }

        counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        monthTypeCounts[monthKey] = counts;

        return counts;
    }

    private static void IncrementMonthCount(
        Dictionary<string, Dictionary<string, int>> monthTypeCounts,
        DateOnly date,
        string type)
    {
        Dictionary<string, int> counts = GetMonthCounts(monthTypeCounts, date);
        counts[type] = counts.GetValueOrDefault(type) + 1;
    }

    private static IReadOnlyDictionary<string, int> GetRecentTypeCounts(IEnumerable<TypeHistoryEntry> history, DateOnly sunday)
    {
        DateOnly windowStart = sunday.AddDays(-RollingTypeWindowDays + 1);

        return history
            .Where(entry => entry.Date >= windowStart)
            .GroupBy(entry => entry.Type, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key.Trim(), group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> GetRecentTypes(IReadOnlyList<TypeHistoryEntry> history)
    {
        if (history.Count == 0)
        {
            return [];
        }

        return history.Count == 1 ? new List<string> { history[^1].Type } : new List<string> { history[^2].Type, history[^1].Type };
    }

    private (Psalm? Psalm, string Rule) ApplyRules(ScheduleContext context)
    {
        foreach (IReadingRule rule in _rules)
        {
            if (!rule.CanApply(context))
            {
                continue;
            }

            Psalm? selected = rule.Select(context);
            if (selected is not null)
            {
                return (selected, rule.Name);
            }
        }

        return (null, string.Empty);
    }

    private static IEnumerable<DateOnly> EnumerateSundays(DateOnly start, DateOnly end)
    {
        DateOnly current = start;
        int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)current.DayOfWeek + 7) % 7;
        current = current.AddDays(daysUntilSunday);

        while (current < end)
        {
            yield return current;
            current = current.AddDays(7);
        }
    }

    private static bool IsFirstSundayOfMonth(DateOnly date) =>
        date.Day <= 7;

    private static bool IsFirstSundayOfYear(DateOnly date) =>
        date.Month == 1 && IsFirstSundayOfMonth(date);

    private static HashSet<DateOnly> GetHolyWeekSundays(IEnumerable<DateOnly> sundays)
    {
        HashSet<DateOnly> set = new();
        IEnumerable<IGrouping<int, DateOnly>> byYear = sundays.GroupBy(s => s.Year);

        foreach (IGrouping<int, DateOnly> group in byYear)
        {
            DateOnly easter = CalculateEasterSunday(group.Key);
            DateOnly palmSunday = easter.AddDays(-7);
            DateOnly sundayAfterEaster = easter.AddDays(7);

            foreach (DateOnly candidate in group)
            {
                if (candidate == palmSunday || candidate == easter || candidate == sundayAfterEaster)
                {
                    set.Add(candidate);
                }
            }
        }

        return set;
    }

    private static HashSet<DateOnly> GetLastTwoSundaysOfNovember(IEnumerable<DateOnly> sundays)
    {
        HashSet<DateOnly> set = new();
        IEnumerable<IGrouping<int, DateOnly>> byYear = sundays.Where(s => s.Month == 11).GroupBy(s => s.Year);

        foreach (IGrouping<int, DateOnly> group in byYear)
        {
            List<DateOnly> sorted = group.OrderBy(s => s.Day).ToList();
            if (sorted.Count < 2)
            {
                continue;
            }

            set.Add(sorted[^1]);
            set.Add(sorted[^2]);
        }

        return set;
    }

    private static DateOnly CalculateEasterSunday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }

    private sealed record TypeHistoryEntry(DateOnly Date, string Type);

    private static string NormalizeType(string? type) =>
        string.IsNullOrWhiteSpace(type) ? string.Empty : type.Trim();

    private static string GetMonthKey(DateOnly date) =>
        $"{date.Year:D4}-{date.Month:D2}";
}
