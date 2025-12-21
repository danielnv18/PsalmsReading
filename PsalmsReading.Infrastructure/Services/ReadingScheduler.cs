using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services;

public sealed class ReadingScheduler(
    IPsalmRepository psalmRepository,
    IReadingRepository readingRepository,
    Random? random = null)
    : IReadingScheduler
{
    private static readonly HashSet<int> HolyWeekPreferredIds = [113, 114, 115, 116, 117, 118];

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
            .Where(IsAllowedPsalm)
            .ToDictionary(p => p.Id);

        IReadOnlyDictionary<int, int> readCounts = (await readingRepository.GetAllAsync(cancellationToken))
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

            var context = new ScheduleContext(
                sunday,
                available,
                readCounts,
                holyWeekSundays.Contains(sunday),
                thanksgivingSundays.Contains(sunday),
                sunday.Month == 12,
                IsFirstSundayOfMonth(sunday),
                IsFirstSundayOfYear(sunday),
                _random);

            (Psalm? chosen, var appliedRule) = ApplyRules(context);

            if (chosen is null)
            {
                continue;
            }

            usedPsalmIds.Add(chosen.Id);
            planned.Add(new PlannedReading(Guid.NewGuid(), chosen.Id, sunday, appliedRule));
        }

        return planned;
    }

    private static bool IsAllowedPsalm(Psalm psalm) =>
        PsalmRules.IsReadable(psalm);

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

    private static Psalm? SelectByTheme(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts, string value, Random random) =>
        SelectBestByTier(candidates.Where(p => p.Themes.Any(t => MatchesNormalized(t, value))), readCounts, random);

    private static Psalm? SelectByTypeThemeOrEpigraph(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts, string value, Random random)
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

    private static Psalm? SelectBestByTier(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts, Random random)
    {
        var candidateList = candidates.ToList();
        if (candidateList.Count == 0)
        {
            return null;
        }

        var groupedByReadCount = candidateList
            .GroupBy(p => readCounts.GetValueOrDefault(p.Id))
            .OrderBy(g => g.Key)
            .ToList();

        foreach (List<Psalm> tierList in groupedByReadCount.Select(tier => tier.ToList()).Where(tierList => tierList.Count != 0))
        {
            if (tierList.Count <= 2)
            {
                return tierList[0];
            }

            var randomIndex = random.Next(tierList.Count);
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

    private sealed record ScheduleContext(
        DateOnly Sunday,
        IReadOnlyList<Psalm> AvailablePsalms,
        IReadOnlyDictionary<int, int> ReadCounts,
        bool IsHolyWeek,
        bool IsThanksgivingSunday,
        bool IsDecember,
        bool IsFirstSundayOfMonth,
        bool IsFirstSundayOfYear,
        Random Random);

    private interface IReadingRule
    {
        string Name { get; }
        bool CanApply(ScheduleContext context);
        Psalm? Select(ScheduleContext context);
    }

    private sealed class FirstSundayOfYearRule : IReadingRule
    {
        public string Name => "First Sunday new year";

        public bool CanApply(ScheduleContext context) => context.IsFirstSundayOfYear;

        public Psalm? Select(ScheduleContext context) =>
            SelectByTheme(context.AvailablePsalms, context.ReadCounts, "Días festivos: año nuevo", context.Random);
    }

    private sealed class ThanksgivingRule : IReadingRule
    {
        public string Name => "Thanksgiving";

        public bool CanApply(ScheduleContext context) => context.IsThanksgivingSunday;

        public Psalm? Select(ScheduleContext context) =>
            SelectByTheme(context.AvailablePsalms, context.ReadCounts, "Días festivos: Agradecimiento", context.Random);
    }

    private sealed class HolyWeekRule(HashSet<int> preferredIds) : IReadingRule
    {
        public string Name => "HolyWeek";

        public bool CanApply(ScheduleContext context) => context.IsHolyWeek;

        public Psalm? Select(ScheduleContext context) =>
            SelectBestByTier(context.AvailablePsalms.Where(p => preferredIds.Contains(p.Id)), context.ReadCounts, context.Random);
    }

    private sealed class DecemberRule : IReadingRule
    {
        public string Name => "Christmas season";

        public bool CanApply(ScheduleContext context) => context.IsDecember;

        public Psalm? Select(ScheduleContext context) =>
            SelectByTypeThemeOrEpigraph(context.AvailablePsalms, context.ReadCounts, "mesiánico", context.Random);
    }

    private sealed class FirstSundayPraiseRule : IReadingRule
    {
        public string Name => "First Sunday of worship";

        public bool CanApply(ScheduleContext context) => context.IsFirstSundayOfMonth;

        public Psalm? Select(ScheduleContext context) =>
            SelectByTypeThemeOrEpigraph(context.AvailablePsalms, context.ReadCounts, "alabanza", context.Random);
    }

    private sealed class GeneralRule : IReadingRule
    {
        public string Name => "General";

        public bool CanApply(ScheduleContext context) => true;

        public Psalm? Select(ScheduleContext context) =>
            SelectBestByTier(context.AvailablePsalms, context.ReadCounts, context.Random);
    }
}
