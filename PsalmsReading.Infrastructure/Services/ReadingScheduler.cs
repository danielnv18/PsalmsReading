using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services;

public sealed class ReadingScheduler(
    IPsalmRepository psalmRepository,
    IReadingRepository readingRepository,
    Random? random = null)
    : IReadingScheduler
{
    private static readonly HashSet<int> ExcludedPsalmIds = [35, 55, 59, 69, 79, 109, 137];
    private static readonly HashSet<int> HolyWeekPreferredIds = [113, 114, 115, 116, 117, 118];

    private readonly Random _random = random ?? new Random();

    public async Task<IReadOnlyList<PlannedReading>> GenerateScheduleAsync(DateOnly startDate, int months, CancellationToken cancellationToken = default)
    {
        if (months <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(months), "Months must be positive.");
        }

        var psalms = (await psalmRepository.GetAllAsync(cancellationToken))
            .Where(IsAllowedPsalm)
            .ToDictionary(p => p.Id);

        var readCounts = (await readingRepository.GetAllAsync(cancellationToken))
            .GroupBy(r => r.PsalmId)
            .ToDictionary(g => g.Key, g => g.Count());

        DateOnly endDate = startDate.AddMonths(months);
        var sundays = EnumerateSundays(startDate, endDate).ToList();
        HashSet<DateOnly> holyWeekSundays = GetHolyWeekSundays(sundays);
        HashSet<DateOnly> thanksgivingSundays = GetLastTwoSundaysOfNovember(sundays);

        var planned = new List<PlannedReading>();
        var usedPsalmIds = new HashSet<int>();

        foreach (DateOnly sunday in sundays)
        {
            var available = psalms.Values.Where(p => !usedPsalmIds.Contains(p.Id)).ToList();
            if (available.Count == 0)
            {
                break;
            }

            Psalm? chosen;
            string ruleApplied;

            if (IsFirstSundayOfYear(sunday))
            {
                chosen = SelectByTheme(available, readCounts, "Días festivos: año nuevo");
                ruleApplied = "First Sunday new year";
            }
            else if (thanksgivingSundays.Contains(sunday))
            {
                chosen = SelectByTheme(available, readCounts, "Días festivos: Agradecimiento");
                ruleApplied = "Thanksgiving";
            }
            else if (holyWeekSundays.Contains(sunday))
            {
                chosen = SelectBestByTier(available.Where(p => HolyWeekPreferredIds.Contains(p.Id)), readCounts);
                ruleApplied = "HolyWeek";
            }
            else if (sunday.Month == 12)
            {
                chosen = SelectByTypeThemeOrEpigraph(available, readCounts, "mesiánico");
                ruleApplied = "Christmas season";
            }
            else if (IsFirstSundayOfMonth(sunday))
            {
                chosen = SelectByTypeThemeOrEpigraph(available, readCounts, "alabanza");
                ruleApplied = "First Sunday of worship";
            }
            else
            {
                chosen = SelectBestByTier(available, readCounts);
                ruleApplied = "General";
            }

            // Fallbacks if a special rule yielded no candidate.
            if (chosen is null)
            {
                chosen = SelectBestByTier(available, readCounts);
                ruleApplied = "General";
            }

            if (chosen is null)
            {
                continue;
            }

            usedPsalmIds.Add(chosen.Id);
            planned.Add(new PlannedReading(Guid.NewGuid(), chosen.Id, sunday, ruleApplied));
        }

        return planned;
    }

    private static bool IsAllowedPsalm(Psalm psalm) =>
        psalm.IsShortReadingCandidate(30) && !ExcludedPsalmIds.Contains(psalm.Id);

    private static IEnumerable<DateOnly> EnumerateSundays(DateOnly start, DateOnly end)
    {
        DateOnly current = start;
        // Move to the first Sunday on or after start
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)current.DayOfWeek + 7) % 7;
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
        var set = new HashSet<DateOnly>();
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
        var set = new HashSet<DateOnly>();
        IEnumerable<IGrouping<int, DateOnly>> byYear = sundays.Where(s => s.Month == 11).GroupBy(s => s.Year);

        foreach (IGrouping<int, DateOnly> group in byYear)
        {
            var sorted = group.OrderBy(s => s.Day).ToList();
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
        // Meeus/Jones/Butcher algorithm
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }

    private Psalm? SelectByTypeThemeOrEpigraph(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts, string value)
    {
        IEnumerable<Psalm> psalms = candidates as Psalm[] ?? candidates.ToArray();
        IEnumerable<Psalm> byType = psalms.Where(p => MatchesNormalized(p.Type, value));
        Psalm? selected = SelectBestByTier(byType, readCounts);
        if (selected is not null)
        {
            return selected;
        }

        IEnumerable<Psalm> byTheme = psalms.Where(p => p.Themes.Any(t => MatchesNormalized(t, value)));
        selected = SelectBestByTier(byTheme, readCounts);
        if (selected is not null)
        {
            return selected;
        }

        IEnumerable<Psalm> byEpigraph = psalms.Where(p => p.Epigraphs.Any(e => MatchesNormalized(e, value)));
        return SelectBestByTier(byEpigraph, readCounts);
    }

    private Psalm? SelectByTheme(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts, string value)
    {
        IEnumerable<Psalm> byTheme = candidates.Where(p => p.Themes.Any(t => MatchesNormalized(t, value)));
        return SelectBestByTier(byTheme, readCounts);
    }

    private Psalm? SelectBestByTier(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts)
    {
        var candidateList = candidates.ToList();
        if (candidateList.Count == 0)
        {
            return null;
        }

        // Group by read count, then iterate through tiers in order (least reads first)
        var groupedByReadCount = candidateList
            .GroupBy(p => readCounts.GetValueOrDefault(p.Id))
            .OrderBy(g => g.Key)
            .ToList();

        foreach (List<Psalm> tierList in groupedByReadCount.Select(tier => tier.ToList()).Where(tierList => tierList.Count != 0))
        {
            // If 1 or 2 psalms in tier, return the first
            if (tierList.Count <= 2)
            {
                return tierList[0];
            }

            // If >2 psalms in tier, return a random one
            var randomIndex = _random.Next(tierList.Count);
            return tierList[randomIndex];
        }

        return null;
    }

    private static bool MatchesNormalized(string? source, string target)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        return string.Equals(Normalize(source), Normalize(target), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        IEnumerable<char> filtered = normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(filtered.ToArray()).ToLowerInvariant().Trim();
    }
}
