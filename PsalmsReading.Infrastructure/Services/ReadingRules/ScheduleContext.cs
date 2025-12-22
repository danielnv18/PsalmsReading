using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Infrastructure.Services.ReadingRules;

public sealed record ScheduleContext(
    DateOnly Sunday,
    IReadOnlyList<Psalm> AvailablePsalms,
    IReadOnlyDictionary<int, int> ReadCounts,
    IReadOnlyDictionary<string, TypeBalanceStats> TypeBalances,
    int MaxTypeTotalReadable,
    IReadOnlyDictionary<string, int> RecentTypeCounts,
    int RecentTotalCount,
    IReadOnlyDictionary<string, int> MonthTypeCounts,
    IReadOnlyList<string> RecentTypes,
    bool IsHolyWeek,
    bool IsThanksgivingSunday,
    bool IsDecember,
    bool IsFirstSundayOfMonth,
    bool IsFirstSundayOfYear,
    Random Random);

public sealed record TypeBalanceStats(string Type, int TotalReadable, int ReadCoverageCount)
{
    public double RemainingRatio =>
        TotalReadable == 0 ? 0d : (double)(TotalReadable - ReadCoverageCount) / TotalReadable;
}
