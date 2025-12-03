using PsalmsReading.Application.Interfaces;
using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services;

public sealed class ReadingScheduler : IReadingScheduler
{
    private static readonly HashSet<int> ExcludedPsalmIds = new([35, 55, 59, 69, 79, 109, 137]);
    private static readonly HashSet<int> HolyWeekPreferredIds = new([113, 114, 115, 116, 117, 118]);

    private readonly IPsalmRepository _psalmRepository;
    private readonly IReadingRepository _readingRepository;

    public ReadingScheduler(IPsalmRepository psalmRepository, IReadingRepository readingRepository)
    {
        _psalmRepository = psalmRepository;
        _readingRepository = readingRepository;
    }

    public async Task<IReadOnlyList<PlannedReading>> GenerateScheduleAsync(DateOnly startDate, int months, CancellationToken cancellationToken = default)
    {
        if (months <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(months), "Months must be positive.");
        }

        var psalms = (await _psalmRepository.GetAllAsync(cancellationToken))
            .Where(IsAllowedPsalm)
            .ToDictionary(p => p.Id);

        var readCounts = (await _readingRepository.GetAllAsync(cancellationToken))
            .GroupBy(r => r.PsalmId)
            .ToDictionary(g => g.Key, g => g.Count());

        var endDate = startDate.AddMonths(months);
        var sundays = EnumerateSundays(startDate, endDate).ToList();
        var holyWeekSundays = GetHolyWeekSundays(sundays);

        var planned = new List<PlannedReading>();
        var usedPsalmIds = new HashSet<int>();

        foreach (var sunday in sundays)
        {
            var available = psalms.Values.Where(p => !usedPsalmIds.Contains(p.Id)).ToList();
            if (available.Count == 0)
            {
                break;
            }

            Psalm? chosen;
            string ruleApplied;

            if (holyWeekSundays.Contains(sunday))
            {
                chosen = SelectBest(available.Where(p => HolyWeekPreferredIds.Contains(p.Id)), readCounts);
                ruleApplied = "HolyWeek";
            }
            else if (sunday.Month == 12)
            {
                chosen = SelectByTypeOrTheme(available, readCounts, "mesiánico");
                ruleApplied = "December mesiánico";
            }
            else if (IsFirstSundayOfMonth(sunday))
            {
                chosen = SelectByTypeOrTheme(available, readCounts, "alabanza");
                ruleApplied = "First Sunday alabanza";
            }
            else
            {
                chosen = SelectBest(available, readCounts);
                ruleApplied = "General";
            }

            // Fallbacks if a special rule yielded no candidate.
            if (chosen is null)
            {
                chosen = SelectBest(available, readCounts);
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
        var current = start;
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

    private static HashSet<DateOnly> GetHolyWeekSundays(IEnumerable<DateOnly> sundays)
    {
        var set = new HashSet<DateOnly>();
        var byYear = sundays.GroupBy(s => s.Year);

        foreach (var group in byYear)
        {
            var easter = CalculateEasterSunday(group.Key);
            var palmSunday = easter.AddDays(-7);
            var sundayAfterEaster = easter.AddDays(7);

            foreach (var candidate in group)
            {
                if (candidate == palmSunday || candidate == easter || candidate == sundayAfterEaster)
                {
                    set.Add(candidate);
                }
            }
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

    private static Psalm? SelectByTypeOrTheme(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts, string value)
    {
        var byType = candidates.Where(p => p.HasType(value));
        var selected = SelectBest(byType, readCounts);
        if (selected is not null)
        {
            return selected;
        }

        var byTheme = candidates.Where(p => p.HasTheme(value));
        return SelectBest(byTheme, readCounts);
    }

    private static Psalm? SelectBest(IEnumerable<Psalm> candidates, IReadOnlyDictionary<int, int> readCounts)
    {
        return candidates
            .OrderBy(p => readCounts.GetValueOrDefault(p.Id))
            .ThenBy(p => p.TotalVerses)
            .ThenBy(p => p.Id)
            .FirstOrDefault();
    }
}
